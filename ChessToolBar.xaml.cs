using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;

namespace SrcChess2 {
    /// <summary>
    /// Toolbar for the Chess Program
    /// </summary>
    

    /// <summary>
    /// Defines a toolbar button
    /// </summary>
    public class ToolBarButton : Button {
        /// <summary>Image dependency property</summary>
        public static readonly DependencyProperty ImageProperty;
        /// <summary>Image Disabled dependency property</summary>
        public static readonly DependencyProperty DisabledImageProperty;
        /// <summary>Flip dependency property</summary>
        public static readonly DependencyProperty FlipProperty;
        /// <summary>Image dependency property</summary>
        public static readonly DependencyProperty TextProperty;
        /// <summary>DisplayStyle dependency property</summary>
        public static readonly DependencyProperty DisplayStyleProperty;
        /// <summary>Inner Image control</summary>
        private Image                             m_imageCtrl;
        /// <summary>Inner Text control</summary>
        private TextBlock                         m_textCtrl;

        /// <summary>Display Style applied to the Toolbarbutton</summary>
        public enum DisplayStyleE {
            /// <summary>Image only displayed</summary>
            Image,
            /// <summary>Text only displayed</summary>
            Text,
            /// <summary>Image and Text displayed</summary>
            ImageAndText
        }

        /// <summary>
        /// Class ctor
        /// </summary>
        static ToolBarButton() {
            ImageProperty           = DependencyProperty.Register("Image",
                                                                  typeof(ImageSource),
                                                                  typeof(ToolBarButton),
                                                                  new FrameworkPropertyMetadata((ImageSource)null,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                                ImageChanged));
            DisabledImageProperty   = DependencyProperty.Register("DisabledImage",
                                                                  typeof(ImageSource),
                                                                  typeof(ToolBarButton),
                                                                  new FrameworkPropertyMetadata((ImageSource)null,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                                DisabledImageChanged));
            FlipProperty            = DependencyProperty.Register("Flip",
                                                                  typeof(bool),
                                                                  typeof(ToolBarButton),
                                                                  new FrameworkPropertyMetadata(false,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                                FlipChanged));
            TextProperty            = DependencyProperty.Register("Text",
                                                                  typeof(String),
                                                                  typeof(ToolBarButton),
                                                                  new FrameworkPropertyMetadata("",
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                                                                TextChanged));
            DisplayStyleProperty    = DependencyProperty.RegisterAttached("DisplayStyle",
                                                                         typeof(DisplayStyleE),
                                                                         typeof(ToolBarButton),
                                                                         new FrameworkPropertyMetadata(DisplayStyleE.Text,
                                                                                                       FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                                                                       DisplayStyleChanged));
            IsEnabledProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(IsEnabledChanged)));
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        public ToolBarButton() : base() {
            Style = new Style(typeof(ToolBarButton), (Style)FindResource(ToolBar.ButtonStyleKey));
            BuildInnerButton();
        }

        /// <summary>
        /// Called when Image property changed
        /// </summary>
        private static void ImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ToolBarButton me;

            me = obj as ToolBarButton;
            if (me != null && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Image displayed in button")]
        public ImageSource Image {
            get {
                return ((ImageSource)GetValue(ImageProperty));
            }
            set {
                SetValue(ImageProperty, value);
            }
        }

        /// <summary>
        /// Called when Disabled Image property changed
        /// </summary>
        private static void DisabledImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ToolBarButton me;

            me = obj as ToolBarButton;
            if (me != null && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Disabled Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Disabled Image displayed in button")]
        public ImageSource DisabledImage {
            get {
                return ((ImageSource)GetValue(DisabledImageProperty));
            }
            set {
                SetValue(DisabledImageProperty, value);
            }
        }

        /// <summary>
        /// Called when Flip property changed
        /// </summary>
        private static void FlipChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ToolBarButton me;

            me = obj as ToolBarButton;
            if (me != null && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Flip the image horizontally
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Flip horizontally the Image displayed in button")]
        public bool Flip {
            get {
                return ((bool)GetValue(FlipProperty));
            }
            set {
                SetValue(FlipProperty, value);
            }
        }

        /// <summary>
        /// Called when Text property changed
        /// </summary>
        private static void TextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ToolBarButton me;

            me = obj as ToolBarButton;
            if (me != null && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Text displayed in button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Text displayed in button")]
        public String Text {
            get {
                return ((String)GetValue(TextProperty));
            }
            set {
                SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Called when DisplayStyle property changed
        /// </summary>
        private static void DisplayStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ToolBarButton     tbItem;

            tbItem  = obj as ToolBarButton;
            if (e.OldValue != e.NewValue && tbItem != null) {
                tbItem.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Display Style applied to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Display Style applied to the button")]
        public DisplayStyleE DisplayStyle {
            get {
                return ((DisplayStyleE)GetValue(DisplayStyleProperty));
            }
            set {
                SetValue(DisplayStyleProperty, value);
            }
        }

        /// <summary>
        /// Set the Display Style
        /// </summary>
        /// <param name="element">      Dependency element</param>
        /// <param name="eDisplayStyle">Display Style</param>
        public static void SetDisplayStyle(DependencyObject element, DisplayStyleE eDisplayStyle) {
            if (element == null) {
                throw new ArgumentNullException("element");
            }
            element.SetValue(DisplayStyleProperty, eDisplayStyle);
        }

        /// <summary>
        /// Get the full name of the field attached to a column
        /// </summary>
        /// <param name="element">  Dependency element</param>
        /// <returns>
        /// Field full name
        /// </returns>
        public static DisplayStyleE GetDisplayStyle(DependencyObject element) {
            if (element == null) {
                throw new ArgumentNullException("element");
            }
            return((DisplayStyleE)element.GetValue(DisplayStyleProperty));
        }

        /// <summary>
        /// Called when IsEnabled property changed
        /// </summary>
        private new static void IsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ToolBarButton me;

            me = obj as ToolBarButton;
            if (me != null && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Set the source image depending the enabled state
        /// </summary>
        /// <param name="bFlip">    true if flipped</param>
        private void SetImage(bool bFlip) {
            ScaleTransform  scaleTransform;

            m_imageCtrl.Source      = (IsEnabled) ? Image : DisabledImage;
            m_imageCtrl.OpacityMask = null;
            if (bFlip) {
                m_imageCtrl.RenderTransformOrigin   = new Point(0.5, 0.5);
                scaleTransform                      = new ScaleTransform();
                scaleTransform.ScaleX               = -1;
                m_imageCtrl.RenderTransform         = scaleTransform;
            }
        }

        /// <summary>
        /// Builds the inner controls to make the button
        /// </summary>
        private void BuildInnerButton() {
            Grid            grid;

            grid                     = new Grid();
            grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            m_imageCtrl              = new Image();
            m_textCtrl               = new TextBlock() { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = System.Windows.VerticalAlignment.Center };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(m_imageCtrl, 0);
            grid.Children.Add(m_imageCtrl);
            Grid.SetColumn(m_textCtrl, 1);
            grid.Children.Add(m_textCtrl);
            Content                 = grid;
        }

        /// <summary>
        /// Updates the inner controls of the button
        /// </summary>
        private void UpdateInnerButton() {
            DisplayStyleE   eDisplayStyle;
            Grid            grid;
            String          strText;

            grid          = (Grid)Content;
            eDisplayStyle = DisplayStyle;
            strText       = Text;
            if (Image != null && (eDisplayStyle == DisplayStyleE.Image || eDisplayStyle == DisplayStyleE.ImageAndText)) {
                grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                SetImage(Flip);
            } else {
                m_imageCtrl.Source              = null;
                grid.ColumnDefinitions[0].Width = new GridLength(0);
            }
            if (!String.IsNullOrEmpty(strText) && (eDisplayStyle == DisplayStyleE.Text || eDisplayStyle == DisplayStyleE.ImageAndText)) {
                grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                m_textCtrl.Text                 = strText;
            } else {
                m_textCtrl.Text                 = String.Empty;
                grid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        } 
    }
}
