using ReaCS.Runtime.Registries;
using static ReaCS.Runtime.Access;

namespace ReaCS.Runtime.Core
{
    public abstract class LinkSO : ObservableScriptableObject
    {
        public abstract ObservableScriptableObject Left { get; }
        public abstract ObservableScriptableObject Right { get; }
    }

    public class LinkSO<TLeft, TRight> : LinkSO, ILinkResettable
    where TLeft : ObservableScriptableObject
    where TRight : ObservableScriptableObject
    {
        // These are properly drawn now thanks to ObservableSOPropertyDrawer
        public ObservableSO<TLeft> LeftSO = new();
        public ObservableSO<TRight> RightSO = new();

        public override ObservableScriptableObject Left => LeftSO.Value;
        public override ObservableScriptableObject Right => RightSO.Value;

        private bool _isRegistered;

        public void ClearLink()
        {
            LeftSO.Value = null;
            RightSO.Value = null;
            _isRegistered = false;
        }
        public LinkSO<TLeft, TRight> SetLinks(TLeft left, TRight right)
        {
            LeftSO.Value = left;
            RightSO.Value = right; 
            return this;
        }

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!_isRegistered)
            {
                Query<LinkSORegistry>().Register(this);
                _isRegistered = true;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_isRegistered)
            {
                Query<LinkSORegistry>().Unregister(this);
                _isRegistered = false;
            }
        }
#endif
    }
}