using NUnit.Framework;
using ReaCS.Runtime;
using System;

namespace ReaCS.Tests.Runtime
{
    public class ObservableAttributeTests
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