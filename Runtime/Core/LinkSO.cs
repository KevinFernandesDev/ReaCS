using ReaCS.Runtime.Registries;
using static ReaCS.Runtime.ReaCS;

namespace ReaCS.Runtime.Core
{
    public abstract class LinkSO : ObservableScriptableObject
    {
        public abstract ObservableScriptableObject Left { get; }
        public abstract ObservableScriptableObject Right { get; }
    }

    public class LinkSO<TLeft, TRight> : LinkSO
    where TLeft : ObservableScriptableObject
    where TRight : ObservableScriptableObject
    {
        // These are properly drawn now thanks to ObservableSOPropertyDrawer
        public ObservableSO<TLeft> LeftSO = new();
        public ObservableSO<TRight> RightSO = new();

        public override ObservableScriptableObject Left => LeftSO.Value;
        public override ObservableScriptableObject Right => RightSO.Value;

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
            Query<LinkSORegistry>().Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Query<LinkSORegistry>().Unregister(this);
        }
#endif
    }
}