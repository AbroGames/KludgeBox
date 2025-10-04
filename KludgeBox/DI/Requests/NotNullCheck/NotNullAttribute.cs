namespace KludgeBox.DI.Requests.NotNullCheck;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NotNullAttribute : Attribute
{
    public bool ThrowOnFail { get; }

    public NotNullAttribute(bool throwOnFail)
    {
        ThrowOnFail = throwOnFail;
    }

    public NotNullAttribute() : this(false) {}
}

public class NotNullStrictAttribute : NotNullAttribute
{
    public NotNullStrictAttribute() : base(true) {}
}