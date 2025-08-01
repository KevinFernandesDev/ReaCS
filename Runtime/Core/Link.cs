using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;
using static ReaCS.Runtime.Access;

namespace ReaCS.Runtime.Core
{
    public abstract class LinkBase : ObservableObject
    {
        public abstract ObservableObject Left { get; }
        public abstract ObservableObject Right { get; }
    }

    public class Link<TLeft, TRight> : LinkBase, ILinkResettable, ILinkConnector
     where TLeft : ObservableObject
     where TRight : ObservableObject
    {
        public ObservableSO<TLeft> LeftSO = new();
        public ObservableSO<TRight> RightSO = new();

        public override ObservableObject Left => LeftSO.Value;
        public override ObservableObject Right => RightSO.Value;

        public void ClearLink()
        {
            LeftSO.Value = null;
            RightSO.Value = null;
        }

        public Link<TLeft, TRight> SetLinks(TLeft left, TRight right)
        {
            LeftSO.Value = left;
            RightSO.Value = right;
            return this;
        }

        // Add the generic setter required by the pool
        public void Connect(ObservableObject left, ObservableObject right)
        {
            // Use 'as' to safely cast and provide good errors
            if (left is not TLeft tLeft)
                throw new System.ArgumentException($"Left must be of type {typeof(TLeft).Name}");
            if (right is not TRight tRight)
                throw new System.ArgumentException($"Right must be of type {typeof(TRight).Name}");
            SetLinks(tLeft, tRight);
        }

        public override void RegisterSelf()
        {
            Access.Query<LinkRegistry>().Register(this);
        }

        public override void UnregisterSelf()
        {
            Access.Query<LinkRegistry>().Unregister(this);
        }
    }
}