using System;
using System.Reflection;

namespace OmegaCrosspathing;

public static class ReflectionHelper
{
    public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
    {
        PropertyInfo propInfo = null;
        do
        {
            propInfo = type.GetProperty(propertyName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            type = type.BaseType;
        }
        while (propInfo == null && type != null);
        return propInfo;
    }

    public static object GetPropertyValue(this object obj, string propertyName)
    {
        if (obj == null)
            throw new ArgumentNullException("obj");
        Type objType = obj.GetType();
        PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
        if (propInfo == null)
            throw new ArgumentOutOfRangeException("propertyName",
                $"Couldn't find property {propertyName} in type {objType.FullName}");
        return propInfo.GetValue(obj, null);
    }
}