using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;


namespace mRemoteNG.Root.PuttySessions
{
	public class PuttySessionsNodeInfo : RootNodeInfo
    {
        private string _name;
        private string _panel;


        public PuttySessionsNodeInfo() : base(RootNodeType.PuttySessions)
		{
			_name = Language.strPuttySavedSessionsRootName;
			_panel = Language.strGeneral;
		}

        #region Public Properties
        [LocalizedAttributes.LocalizedDefaultValue("strPuttySavedSessionsRootName")]
        public override string Name
		{
			get { return _name; }
			set
			{
				if (_name == value)
				{
					return ;
				}
				_name = value;
				if (TreeNode != null)
				{
					TreeNode.Text = value;
				}
                Settings.Default.PuttySavedSessionsName = value;
			}
		}
				
        [LocalizedAttributes.LocalizedCategory("strCategoryDisplay"),
            LocalizedAttributes.LocalizedDisplayName("strPropertyNamePanel"),
            LocalizedAttributes.LocalizedDescription("strPropertyDescriptionPanel")]
        public override string Panel
		{
			get { return _panel; }
			set
			{
				if (_panel == value)
				{
					return ;
				}
				_panel = value;
                Settings.Default.PuttySavedSessionsPanel = value;
			}
        }
        #endregion
    }
}