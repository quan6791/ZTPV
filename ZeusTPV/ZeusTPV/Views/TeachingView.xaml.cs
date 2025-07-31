using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    public partial class TeachingView : UserControl
    {
        private SelectionControlView _selectionControlView;
        private IOControlView _ioControlView;
        private ParamsControlView _paramsControlView;
        private MotionControlView _motionControlView;

        public TeachingView()
        {
            InitializeComponent();

            // Chỉ load content khi không phải design time
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                LoadContent();
            }

        }

        private void LoadContent()
        {
            try
            {
                // Load các UserControl chỉ khi runtime
                TeachingLeftUpContent.Content = _selectionControlView ?? new SelectionControlView();
                TeachingLeftDownContent.Content = _ioControlView ?? new IOControlView();
                TeachingRightUpContent.Content = _paramsControlView ?? new ParamsControlView();
                TeachingRightDownContent.Content = _motionControlView ?? new MotionControlView();
            }
            catch (Exception ex)
            {
                // Log error nhưng không crash
                System.Diagnostics.Debug.WriteLine($"Error loading content: {ex.Message}");
            }
        }
    }
}