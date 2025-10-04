namespace KludgeBox.Reflection.Access;

public interface IMemberSetter : IBaseMemberInfo
{
    void SetValue(object target, object value);
}