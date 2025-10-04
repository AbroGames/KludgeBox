namespace KludgeBox.Reflection.Access;

public interface IMemberGetter : IBaseMemberInfo
{
    object GetValue(object target);
}