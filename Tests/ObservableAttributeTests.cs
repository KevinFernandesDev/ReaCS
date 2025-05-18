using NUnit.Framework;
using System;

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