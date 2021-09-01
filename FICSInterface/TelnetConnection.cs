using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SrcChess2.FICSInterface {

    /// <summary>
    /// Telnet Verb
    /// </summary>
    public enum Verbs {
        /// <summary>Ask if option is available</summary>
        WILL = 251,
        /// <summary>Refuse the option</summary>
        WONT = 252,
        /// <summary>Please do it</summary>
        DO = 253,
        /// <summary>Please don't</summary>
        DONT = 254,
        /// <summary>IAC command</summary>
        IAC = 255
    }

    /// <summary>
    /// TELNET options
    /// </summary>
    public enum Options {
        /// <summary>SGA option</summary>
        SGA = 3
    }

    /// <summary>
    /// minimalistic telnet implementation
    /// conceived by Tom Janssens on 2007/06/06  for codeproject
    /// </summary>
    public class TelnetConnection : IDisposable {
        /// <summary>Called when a new text has been received</summary>
        public event EventHandler   NewTextReceived;
        /// <summary>Called when a new line has been received</summary>
        public event EventHandler   NewLineReceived;
        /// <summary>TCP/IP socket</summary>
        private TcpClient           m_tcpSocket;
        /// <summary>Network stream</summary>
        private NetworkStream       m_stream;
        /// <summary>Receiving buffer</summary>
        private byte[]              m_buf;
        /// <summary>Up to one unprocessed byte</summary>
        private byte?               m_bLastByte = null;
        /// <summary>String builder containing the received character</summary>
        private StringBuilder       m_strbInput;
        /// <summary>true if object is listening</summary>
        private bool                m_bListening;
        /// <summary>true to send trace to debugging output</summary>
        private bool                m_bDebugTrace;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bDebugTrace">  true to send send text and received text to the debugger output</param>
        public TelnetConnection(bool bDebugTrace) {
            m_tcpSocket     = null;
            m_strbInput     = null;
            m_strbInput     = new StringBuilder(65536);
            m_bDebugTrace   = bDebugTrace;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public TelnetConnection() : this(false /*bDebugTrace*/) {
        }

        /// <summary>
        /// Disposing the object
        /// </summary>
        /// <param name="bDisposing">   true for dispose, false for finallizing</param>
        protected virtual void Dispose(bool bDisposing) {
            m_bListening = false;
            if (m_stream != null) {
                m_stream.Dispose();
                m_stream = null;
            }
            if (m_tcpSocket != null) {
                m_tcpSocket.Close();
                m_tcpSocket = null;
            }
        }

        /// <summary>
        /// Dispose the connection to the FICS server
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Connect to the port
        /// </summary>
        /// <param name="strHostName">  Host name</param>
        /// <param name="iPort">        Port number</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool Connect(string strHostName, int iPort) {
            bool    bRetVal;
            Action  action;

            if (m_tcpSocket != null) {
                throw new MethodAccessException("Already connected");
            }
            try {
                m_tcpSocket = new TcpClient(strHostName, iPort);
                m_stream    = m_tcpSocket.GetStream();
                m_buf       = new byte[m_tcpSocket.ReceiveBufferSize + 1];
                bRetVal     = true;
            } catch(System.Exception) {
                bRetVal     = false;
            }
            if (bRetVal) {
                action      = ProcessInput;
                Task.Factory.StartNew(action);
            }
            return(bRetVal);
        }

        /// <summary>
        /// true to send debugging information to the debugging output
        /// </summary>
        public bool DebugTrace {
            get {
                return(m_bDebugTrace);
            }
            set {
                m_bDebugTrace = value;
            }
        }

        /// <summary>
        /// Trigger the NewTextReceived event
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected virtual void OnNewTextReceived(EventArgs e) {
            if (NewTextReceived != null) {
                NewTextReceived(this, e);
            }
        }

        /// <summary>
        /// Trigger the NewLineReceived event
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected virtual void OnNewLineReceived(EventArgs e) {
            if (NewLineReceived != null) {
                NewLineReceived(this, e);
            }
        }

        /// <summary>
        /// Send a text to telnet host
        /// </summary>
        /// <param name="strCmd">   Command</param>
        public void Send(string strCmd) {
            byte[]  buf;

            if (m_tcpSocket.Connected) {
                lock(m_stream) {
                    buf = System.Text.ASCIIEncoding.ASCII.GetBytes(strCmd.Replace("\0xFF","\0xFF\0xFF"));
                    m_stream.Write(buf, 0, buf.Length);
                    if (m_bDebugTrace) {
                        System.Diagnostics.Debug.Write(strCmd);
                    }
                }
            }
        }

        /// <summary>
        /// Send a line to telnet host
        /// </summary>
        /// <param name="strCmd">   Command</param>
        public void SendLine(string strCmd) {
            Send(strCmd + "\n\r");
        }

        /// <summary>
        /// Parse the received buffer
        /// </summary>
        private void ParseTelnet(int iByteCount) {
            int     iPos;
            Byte    bInp;
            Byte    bInputVerb;
            Byte    bInputOption;

            iPos    = 0;
            while (iPos < iByteCount) {
                bInp = m_buf[iPos++];
                switch ((Verbs)bInp) {
                case Verbs.IAC:
                    if (iPos < iByteCount) {
                        // interpret as command
                        bInputVerb = m_buf[iPos++];
                        switch ((Verbs)bInputVerb) {
                        case Verbs.IAC: 
                            //literal IAC = 255 escaped, so append char 255 to string
                            m_strbInput.Append(Convert.ToChar(bInputVerb));
                            if (m_bDebugTrace) {
                                System.Diagnostics.Debug.Write(Convert.ToChar(bInputVerb));
                            }
                            break;
                        case Verbs.DO: 
                        case Verbs.DONT:
                        case Verbs.WILL:
                        case Verbs.WONT:
                            // reply to all commands with "WONT", unless it is SGA (suppress go ahead)
                            bInputOption = m_buf[iPos++];
                            if (bInputOption == (int)Options.SGA) {
                                m_stream.WriteByte((Verbs)bInputVerb == Verbs.DO ? (byte)Verbs.WILL: (byte)Verbs.DO);
                            } else {
                                m_stream.WriteByte((Verbs)bInputVerb == Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                            }
                            m_stream.WriteByte(bInputOption);
                            break;
                        default:
                            break;
                        }
                    } else {
                        m_bLastByte = bInp;
                    }
                    break;
                default:
                    if (bInp == '\r') {
                        bInp = (byte)'\n';
                    } else if (bInp == '\n') {
                        bInp = (byte)'\r';
                    }
                    m_strbInput.Append(Convert.ToChar(bInp));
                    if (m_bDebugTrace) {
                        System.Diagnostics.Debug.Write(Convert.ToChar(bInp));
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Read the received data
        /// </summary>
        private void ReadInput() {
            int iReadSize;
            int iOffset;

            iOffset = 0;
            if (m_bLastByte != null) {
                m_buf[iOffset++] = m_bLastByte.Value;
                m_bLastByte      = null;
            }
            iReadSize = m_stream.Read(m_buf, iOffset, m_buf.Length) + iOffset;
            if (iReadSize != 0) {
                ParseTelnet(iReadSize);
            }
        }

        /// <summary>
        /// Process the input
        /// </summary>
        private void ProcessInput() {            
            bool    bTextReceived;
            bool    bLineReceived;

            m_bListening    = true;
            try {
                while (m_bListening && m_tcpSocket != null && m_tcpSocket.Connected && m_stream != null) {
                    lock(m_stream) {
                        while (m_stream.DataAvailable) {
                            ReadInput();
                        }
                        bTextReceived = m_strbInput.Length != 0;
                        bLineReceived = bTextReceived && m_strbInput.ToString().IndexOf('\n') != -1;
                    }
                    if (m_bListening) {
                        if (bTextReceived) {
                            OnNewTextReceived(EventArgs.Empty);
                        }
                        if (bLineReceived) {
                            OnNewLineReceived(EventArgs.Empty);
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                }
            } catch(System.Exception) {
            }
            m_bListening    = false;
        }


        /// <summary>
        /// Returns true if still listening
        /// </summary>
        public bool IsListening {
            get {
                return(m_bListening);
            }
        }

        /// <summary>
        /// Read text already read
        /// </summary>
        /// <returns>
        /// Read text
        /// </returns>
        public string GetAllReadText() {
            string  strRetVal;

            lock(m_stream) {
                strRetVal = m_strbInput.ToString();
                m_strbInput.Clear();
            }
            return(strRetVal);
        }

        /// <summary>
        /// Returns the next already read line
        /// </summary>
        /// <returns>
        /// Next read line or null if not read yet
        /// </returns>
        public string GetNextReadLine() {
            string  strRetVal;
            int     iIndex;

            lock(m_stream) {
                strRetVal   = m_strbInput.ToString();
                iIndex      = strRetVal.IndexOf('\n');
                if (iIndex == -1) {
                    strRetVal = null;
                } else {
                    strRetVal = strRetVal.Substring(0, iIndex).Replace("\r", "");
                    m_strbInput.Remove(0, iIndex + 1);
                }
            }
            return(strRetVal);
        }

        /// <summary>
        /// Flush received buffer
        /// </summary>
        public void FlushInput() {
            lock(m_stream) {
                m_strbInput.Clear();
            }
        }
    }
}
