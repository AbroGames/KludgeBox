using Godot;
using KludgeBox.DI.Requests;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;
using KludgeBox.DI.Requests.ParentInjection;

namespace KludgeTests.Tests.DiTests;

internal partial class DiTestTypes
{
    public partial class ParentNode : Node;
    public partial class ChildNode : Node;
    public partial class SpecificChildNode : ChildNode;
    public partial class DeepSpecificChildNode : ChildNode;
    
    public partial class ParentNodeWithChildrenReferences : ParentNode
    {
        public const string CustomChildName = "CustomName";
        
        [Child] public ChildNode DefaultPublicChildProperty { get; set; }
        [Child] private ChildNode _defaultPrivateChildProperty { get; set; }

        [Child] public ChildNode DefaultPublicChildField;
        [Child] private ChildNode _defaultPrivateChildField;
        
        [Child(By.Name, CustomChildName)] public ChildNode CustomNamedChildProperty { get; set; }
        [Child(By.Type)] public SpecificChildNode ByTypeChildProperty { get; set; }
        [Child(By.Type)] public DeepSpecificChildNode DeepByTypeChildProperty { get; set; }
        [Child] public ChildNode DeepByNameChildField;

    }

    public partial class ParentNodeWithNullStrictNullablePublicMembers : ParentNode
    {
        [NotNullStrict] public object PublicNullObjectProperty { get; set; }
        [NotNullStrict] public object PublicNullObjectField;
    }

    public partial class ParentNodeWithNullStrictNullablePrivateMembers : ParentNode
    {
        [NotNullStrict] private object PrivateNullObjectProperty { get; set; }
        [NotNullStrict] private object PrivateNullObjectField;
    }
    
    public partial class ParentNodeWithStrictNullablePublicMembers : ParentNode
    {
        [NotNullStrict] public object PublicNullObjectProperty { get; set; } = new();
        [NotNullStrict] public object PublicNullObjectField = new();
    }

    public partial class ParentNodeWithStrictNullablePrivateMembers : ParentNode
    {
        [NotNullStrict] private object PrivateNullObjectProperty { get; set; } = new();
        [NotNullStrict] private object PrivateNullObjectField = new();
    }
    
    public partial class ParentNodeWithNullNullablePublicMembers : ParentNode
    {
        [NotNull] public object NonStrictPublicNullObjectProperty { get; set; }
        [NotNull] public object NonStrictPublicNullObjectField;
    }

    public partial class ParentNodeWithNullNullablePrivateMembers : ParentNode
    {
        [NotNull] private object NonStrictPrivateNullObjectProperty { get; set; }
        [NotNull] private object NonStrictPrivateNullObjectField;
    }
}