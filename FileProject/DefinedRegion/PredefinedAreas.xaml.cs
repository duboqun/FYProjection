using PIE.Carto;
using PIE.Display;
using PIE.Geometry;
using PIE.Meteo.Common;
using PIE.Meteo.Core;
using PIE.Meteo.FileProject;
using PIE.Meteo.PIEControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PIE.Meteo.FileProject
{
    /// <summary>
    /// PredefinedAreas.xaml 的交互逻辑
    /// </summary>
    public partial class PredefinedAreas : mModernWindow
    {
        public PredefinedAreas()
        {
            InitializeComponent();
            LoadDataBind();
            var mapControl = mService.MainPanelVM?.CurSelectedDocument as mMapOperation;
            if (mapControl == null)
                btnInteract.IsEnabled = false;
        }
        /// <summary>
        /// 控件数据
        /// </summary>
        private IList<PreTreeModel> _itemsSourceData = new List<PreTreeModel>();

        public event Action<PrjEnvelopeItem, bool> CheckedChanged;

        ///数据绑定
        private void LoadDataBind()
        {
            ObservableCollection<PreTreeModel> pavList = new ObservableCollection<PreTreeModel>();
            DefinedRegionParse drp = new DefinedRegionParse();//预定义范围配置文件解析xml
            BlockDefined blockDefined = drp.BlockDefined;//预定义区域
            for (int i = 0; i < drp.BlockDefined.BlockItemGroups.Count(); i++)//一个区域里的组数
            {
                BlockItemGroup group = blockDefined.BlockItemGroups[i];
                PrjEnvelopeItem[] envs = group.BlockItems;//每个组下面的子项
                PreTreeModel model = new PreTreeModel();//树的每个节点(组)
                model.Id = i.ToString();
                model.Name = string.Format("{0}[{1}]({2})", group.Name, group.Description, envs.Length);
                model.IsExpanded = false;
                model.Tag = group;
                foreach (PrjEnvelopeItem env in envs)//每个组对应的项
                {
                    PreTreeModel child = new PreTreeModel();//每个节点(项的节点)
                    child.Id = $"{model.Id}-{env.Name}";
                    //child.Identify = env.Identify;
                    child.Name = env.Name;
                    child.Tag = env;
                    child.Parent = model;
                    model.Children.Add(child);//往组中添加每一个项的节点
                }

                pavList.Add(model);//往集合中添加每个组
            }
            ItemsSourceData = pavList;//绑定数据来源
        }

        /// <summary>
        /// 保存  按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ObservableCollection<PreTreeModel> pavList = treeView.ItemsSource as ObservableCollection<PreTreeModel>;

            List<BlockItemGroup> groups = new List<BlockItemGroup>();
            foreach (var group in pavList)
            {
                BlockItemGroup bGroup = group.Tag as BlockItemGroup;
                BlockItemGroup newGroup = new BlockItemGroup(bGroup.Name, bGroup.Description, bGroup.Identify);
                foreach (var item in group.Children)
                {
                    PrjEnvelopeItem pItem = item.Tag as PrjEnvelopeItem;
                    newGroup.Add(pItem);
                }
                groups.Add(newGroup);
            }
            BlockDefined db = new BlockDefined(groups.ToArray());
            DefinedRegionParse.Save(db);
            MessageBox.Show("保存成功");

        }


        public IList<PreTreeModel> ItemsSourceData
        {
            get { return _itemsSourceData; }
            set
            {
                _itemsSourceData = value;
                treeView.ItemsSource = _itemsSourceData;
            }
        }
        /// <summary>
        /// Tree选中改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var ptm = treeView.SelectedItem as PreTreeModel;
            if (ptm.Tag is PrjEnvelopeItem)
            {
                PrjEnvelopeItem item = ptm.Tag as PrjEnvelopeItem;
                this.txtName.Text = item.Name;
                this.txtW.Text = item.PrjEnvelope.MinX.ToString();//西经
                this.txtE.Text = item.PrjEnvelope.MaxX.ToString();//东经
                this.txtN.Text = item.PrjEnvelope.MaxY.ToString();//北纬
                this.txtS.Text = item.PrjEnvelope.MinY.ToString();//南纬
            }

        }
        /// <summary>
        /// 点击  新建分组  出来一个窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNewGroup_Click(object sender, RoutedEventArgs e)
        {

            NewGroup ng = new NewGroup();
            if (ng.ShowDialog() == true)
            {
                string name = ng.value1.Text;//分组名称
                string identity = ng.value2.Text;//分组标识

                BlockDefined blockDefined = new BlockDefined();
                BlockItemGroup group = new BlockItemGroup(name);
                group.Identify = identity;
                ObservableCollection<PreTreeModel> models = treeView.ItemsSource as ObservableCollection<PreTreeModel>;
                //foreach (var item in models)9/30
                //{
                //    item.Name = "ewrtgf";
                //}
                if (models != null)
                {
                    PreTreeModel model = new PreTreeModel();
                    model.Id = Guid.NewGuid().ToString();
                    model.Name = string.Format("{0}[{1}]", group.Name, group.Description);

                    model.IsExpanded = false;
                    model.Tag = group;
                    models.Add(model);
                }
                ItemsSourceData = models;//绑定数据来源
            }
        }
        /// <summary>
        /// 删除按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<PreTreeModel> models = treeView.ItemsSource as ObservableCollection<PreTreeModel>;

            var ptm = treeView.SelectedItem as PreTreeModel;
            if (ptm != null)
            {
                //&& ptm.Children.Count>0
                if (ptm.Children != null  && ptm.Parent == null)
                {
                    //窗口级别的删除(组名)
                    models.Remove(ptm);
                    //xml的删除（组名）
                }
                else
                {
                    //窗口级别的删除(子项)
                    ptm.Parent.Children.Remove(ptm);
                    PrjEnvelopeItem prj = ptm.Tag as PrjEnvelopeItem;
                    
                    BlockItemGroup items = ptm.Parent.Tag as BlockItemGroup;
                    items.Remove(prj);
                    models.Remove(ptm);
                    ptm.Parent.Name = string.Format("{0}[{1}]({2})", items.Name, items.Description, ptm.Parent.Children.Count);
                }
            }

        }
        /// <summary>
        /// 新增按钮 子项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddItemButton_Click_1(object sender, RoutedEventArgs e)
        {
            ObservableCollection<PreTreeModel> models = treeView.ItemsSource as ObservableCollection<PreTreeModel>;
            var ptm = treeView.SelectedItem as PreTreeModel;
            if (ptm != null && ptm.Parent ==null)
            {
                NewItems ng = new NewItems();
                if (ng.ShowDialog() == true)
                {
                    string name = ng.value1.Text;//分组子项
                    string identity = ng.value2.Text;//分组子项标识

                    BlockDefined blockDefined = new BlockDefined();

                    PreTreeModel model = new PreTreeModel();
                    model.Id = Guid.NewGuid().ToString();
                    model.Name = name;
                    model.Identify = identity;
                    model.WLon = Convert.ToDouble(ng.txtw.Text);
                    model.ELon = Convert.ToDouble(ng.txte.Text);
                    model.SLat = Convert.ToDouble(ng.txts.Text);
                    model.NLat = Convert.ToDouble(ng.txtn.Text);
                    model.IsExpanded = false;
                    model.Parent = ptm; //10-9给父级赋值
                    
                    PrjEnvelopeItem item = new PrjEnvelopeItem(name, new RasterProject.PrjEnvelope { MaxX = model.ELon, MinX = model.WLon, MaxY = model.NLat, MinY = model.SLat }, identity);
                    model.Tag = item;
                    ptm.Children.Add(model);
                   
                    var group = ptm.Tag as BlockItemGroup;
                    group.Add(item);
                    ptm.Name = string.Format("{0}[{1}]({2})", group.Name, group.Description, group.BlockItems.Length);
                }
            }

        }
        
        /// <summary>
        /// 交互AOI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnInteract_Click(object sender, RoutedEventArgs e)
        {
            var mapControl = mService.MainPanelVM?.CurSelectedDocument as mMapOperation;//mMapOperation
            if(mapControl!=null)
            {
                double ee = 0, w = 0, n = 0, s = 0;
                if(double.TryParse(this.txtE.Text,out ee)&&
                    double.TryParse(this.txtW.Text, out w) &&
                    double.TryParse(this.txtN.Text, out n) &&
                    double.TryParse(this.txtS.Text, out s) )
                {
                    IGeometry geo = this.GetGeoFromCoordinates(n, s, w, ee, mapControl.MapControl.SpatialReference);//Map.SpatialReference
                    SimpleFillSymbol fillSymbol = new SimpleFillSymbol();
                    fillSymbol.Color = System.Drawing.Color.FromArgb(0, 255, 255, 255);
                    fillSymbol.OutlineSymbol = new SimpleLineSymbol();
                    fillSymbol.OutlineSymbol.Color = System.Drawing.Color.Red;
                    fillSymbol.OutlineSymbol.Width = 1;
                    IPolygonElement pEle = new PolygonElement();
                    pEle.Geometry = geo;
                    pEle.Symbol = fillSymbol;
                    mapControl.MapControl.ActiveView.GraphicsContainer.AddElement(pEle);
                    mapControl.MapControl.ActiveView.Extent = geo.GetEnvelope();
                    mapControl.MapControl.ActiveView.PartialRefresh(ViewDrawPhaseType.ViewGraphics);
                }

            }
        }

        /// <summary>
        /// 通过四至点获取envelope
        /// </summary>
        /// <param name="nValue"></param>
        /// <param name="sValue"></param>
        /// <param name="wValue"></param>
        /// <param name="eValue"></param>
        /// <param name="sRef"></param>
        /// <returns></returns>
        private IGeometry GetGeoFromCoordinates(double nValue, double sValue, double wValue, double eValue, ISpatialReference sRef)
        {
            IEnvelope env = new Envelope();
            env.PutCoords(wValue, sValue, eValue, nValue);
            (env as IGeometry).SpatialReference = sRef;
            (env as IGeometry).Transform(sRef);
            return env as IGeometry;
        }

       
        /// <summary>
        /// 更改子节点的的名称和经纬度坐标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApdate_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<PreTreeModel> models = treeView.ItemsSource as ObservableCollection<PreTreeModel>;
            var ptm = treeView.SelectedItem as PreTreeModel;
            if (ptm != null)
            {
                ptm.Name = this.txtName.Text;
                ptm.ELon = Convert.ToDouble(this.txtE.Text);
                ptm.WLon = Convert.ToDouble(this.txtW.Text);
                ptm.NLat = Convert.ToDouble(this.txtN.Text);
                ptm.SLat = Convert.ToDouble(this.txtS.Text);
               
                PrjEnvelopeItem prj = ptm.Tag as PrjEnvelopeItem;
                prj.Name = ptm.Name;
                prj.PrjEnvelope.MinX = ptm.WLon;
                prj.PrjEnvelope.MaxX = ptm.ELon;
                prj.PrjEnvelope.MinY = ptm.SLat;
                prj.PrjEnvelope.MaxY = ptm.NLat;
                BlockItemGroup items = ptm.Parent.Tag as BlockItemGroup;
                
            }
        }
    }
    /// <summary>
    /// 树模型
    /// </summary>
    public class PreTreeModel : INotifyPropertyChanged
    {
        #region 私有变量
        /// <summary>
        /// Id值
        /// </summary>
        private string _id;
        /// <summary>
        /// 显示的名称
        /// </summary>
        private string _name;
        /// <summary>
        /// 标识
        /// </summary>
        private string _identity;
        /// <summary>
        /// 选中状态
        /// </summary>
        private bool _isChecked;
        /// <summary>
        /// 折叠状态
        /// </summary>
        private bool _isExpanded;
        /// <summary>
        /// 子项
        /// </summary>
        private ObservableCollection<PreTreeModel> _children;

        /// <summary>
        /// 西经
        /// </summary>
        private double _wLon;
        public double WLon
        {
            get { return _wLon; }
            set { _wLon = value; }
        }
        /// <summary>
        /// 东经
        /// </summary>
        private double _eLon;
        public double ELon
        {
            get { return _eLon; }
            set { _eLon = value; }
        }
        /// <summary>
        /// 北纬
        /// </summary>
        private double _nLat;
        public double NLat
        {
            get { return _nLat; }
            set { _nLat = value; }
        }
        /// <summary>
        /// 南纬
        /// </summary>
        private double _sLat;
        public double SLat
        {
            get { return _sLat; }
            set { _sLat = value; }
        }

        /// <summary>
        /// 父项
        /// </summary>
        private PreTreeModel _parent;
        #endregion
        public object Tag { get; internal set; }//内部的子节点

        public event Action<PreTreeModel> CheckChange;

        /// <summary>
        /// 设置所有子项的选中状态
        /// </summary>
        /// <param name="isChecked"></param>
        public void SetChildrenChecked(bool isChecked)
        {
            foreach (PreTreeModel child in Children)
            {
                child.IsChecked = IsChecked;
                child.SetChildrenChecked(IsChecked);
            }
        }

        /// <summary>
        /// 构造
        /// </summary>
        public PreTreeModel()
        {
            Children = new ObservableCollection<PreTreeModel>();
            _isChecked = false;
            IsExpanded = false;//没有是否选中
        }

        /// <summary>
        /// 键值
        /// </summary>
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// 显示的字符
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }
        /// <summary>
        /// 标识
        /// </summary>
        public string Identify
        {
            get { return _identity; }
            set { _identity = value; }
        }
        /// <summary>
        /// 指针悬停时的显示说明
        /// </summary>
        public string ToolTip
        {
            get
            {
                return String.Format("{0}-{1}", Id, Name);
            }
        }
        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                if (value != _isChecked)
                    _isChecked = value;
                NotifyPropertyChanged("IsChecked");
                if (_isChecked)
                {
                    if (Parent != null)
                    {
                        Parent.IsChecked = true;
                    }
                }
                else
                {
                    foreach (PreTreeModel child in Children)
                    {
                        child.IsChecked = false;
                    }
                }
                CheckChange?.Invoke(this);
            }
        }
        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    //折叠状态改变
                    _isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }
        /// <summary>
        /// 父项
        /// </summary>
        public PreTreeModel Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }
        /// <summary>
        /// 子项
        /// </summary>
        public ObservableCollection<PreTreeModel> Children
        {
            get { return _children; }
            set { _children = value; }
        }

        /// <summary>
        /// 属性改变事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
