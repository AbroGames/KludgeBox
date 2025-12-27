using System.Linq;
using System.Text;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;
using static KludgeTests.Tests.LocalServices;
namespace KludgeTests.Tests.ReflectionTests;

[TestGroup("Reflection Tests")]
public partial class MembersScanning : TestNode
{
    public override string TestName => "Members scanning";
    [Test]
    public void ScanPublicMembers()
    {
        var instance = new PublicMembersType();
        var members = Scanner.ScanMembers(instance.GetType());

        var sb = new StringBuilder();
        sb.Append($"Found {members.Count} members in {instance.GetType().Name}:");
        foreach (var member in members)
        {
            sb.Append("\n");
            sb.Append($"{member.ValueType.Name} {member.Member.Name} ({(member.IsPublic ? "public" : "NOT public")})");
        }

        const int expectedPublicMembersCount = 2; 
        const int expectedMembersAmount = expectedPublicMembersCount + 1; // 2 public + 1 private backing field for property
        var publicMembersCount = members.Count(member => member.IsPublic);
        
        Assert.AreEqual(expectedMembersAmount, members.Count, $"Expected {expectedMembersAmount} members, but found {members.Count} members");
        Assert.AreEqual(expectedPublicMembersCount, publicMembersCount, $"Expected {expectedPublicMembersCount} public members, but found {publicMembersCount} public members");
        Assert.Success(sb.ToString());
    }

    [Test]
    public void ScanAllMembers()
    {
        var instance = new NonPublicNonPrivateMembersType();
        var members = Scanner.ScanMembers(instance.GetType());
        
        var sb = new StringBuilder();
        sb.Append($"Found {members.Count} members in {instance.GetType().Name}:");
        foreach (var member in members)
        {
            sb.Append("\n");
            sb.Append($"{member.ValueType.Name} {member.Member.Name} ({(member.IsPublic ? "public" : "NOT public")})");
        }
        
        Assert.Success(sb.ToString());
    }

    internal class PublicMembersType
    {
        public string PublicProperty { get; set; }
        public int PublicField;
    }

    internal class PrivateMembersType : PublicMembersType
    {
        private string _privateProperty { get; set; }
        private int _privateField;
    }

    internal class PartiallyPrivatePropertiesType : PrivateMembersType
    {
        public double PublicGetterPrivateSetterProperty { get; private set; }
        //public double PrivateGetterPublicSetterProperty { private get; set; }
        
        //public double ReadPrivateGetterProperty() => PrivateGetterPublicSetterProperty;
    }
    
    internal class NonPublicNonPrivateMembersType : PartiallyPrivatePropertiesType
    {
        protected bool _protectedProperty { get; set; }
        internal long InternalField;
    }
}