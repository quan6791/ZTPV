using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    public partial class TeachingView : UserControl
    {
        public TeachingView()
        {
            InitializeComponent();

            // Chỉ load content khi không phải design time
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                //_jog = new Jog();
                LoadContent();
            }

        }

        private void LoadContent()
        {
            try
            {
                // Load các UserControl chỉ khi runtime
                TeachingLeftUpContent.Content = new SelectionControlView();
                TeachingLeftDownContent.Content = new IOControlView();
                TeachingRightUpContent.Content = new ParamsControlView();
                TeachingRightDownContent.Content = new MotionControlView();
            }
            catch (Exception ex)
            {
                // Log error nhưng không crash
                System.Diagnostics.Debug.WriteLine($"Error loading content: {ex.Message}");
            }
        }
    }
}