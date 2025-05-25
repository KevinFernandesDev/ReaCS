using NUnit.Framework;
using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using System;

namespace ReaCS.Tests.EditMode
{
    public class ObservableAttribute_EditModeTests
    {
        private class AttributeSO : ObservableScriptableObject
        {
            [Observable] public Observable<int> number;
        }

        [Test]
        public void ObservableAttribute_Applied_Correctly()
        {
            var field = typeof(AttributeSO).GetField("number");
            Assert.IsTrue(Attribute.IsDefined(field, typeof(ObservableAttribute)));
        }
    }
}