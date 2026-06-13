namespace Calculator.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MathsTests
{
    [TestMethod]
    public void Add_ReturnsSum() => Assert.AreEqual(5, Maths.Add(2, 3));

    [TestMethod]
    public void Triple_UsesInternalMember_ViaInternalsVisibleTo() =>
        Assert.AreEqual(9, Maths.Triple(3));
}
