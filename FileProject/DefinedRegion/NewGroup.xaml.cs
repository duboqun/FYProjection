using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PIE.Meteo.FileProject
{
    /// <summary>
    /// NewGroup.xaml 的交互逻辑
    /// </summary>
    public partial class NewGroup : Window
    {
        public NewGroup()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 分组名称
        /// </summary>
        private string _value;
        public string Value1
        {
            get { return _value; }
            set { _value = value; }
        }
        /// <summary>
        /// 分组标识
        /// </summary>
        private string _value2;
        public string Value2
        {
            get { return _value2; }
            set { _value2 = value; }
        }
        /// <summary>
        /// 确认按钮 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            this.Close();
        }

       
    }
}
