using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Nustache.Core
{
    public abstract class ValueGetter
    {
        public static readonly object NoValue = new object();

        #region Static helper methods

        public static object GetValue(object target, string name)
        {
            return GetValueGetter(target, name).GetValue();
        }

        private static ValueGetter GetValueGetter(object target, string name)
        {
            return PropertyInfoValueGetter.GetPropertyInfoValueGetter(target, name)
                ?? (ValueGetter)new NoValueGetter();
        }

        #endregion

        #region Abstract methods

        public abstract object GetValue();

        #endregion

        #region Constants for derived classes that use reflection

        protected const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        protected const StringComparison DefaultNameComparison = StringComparison.CurrentCultureIgnoreCase;

        #endregion
    }

    internal class PropertyInfoValueGetter : ValueGetter
    {
        private static ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _properties = new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();

        internal static PropertyInfoValueGetter GetPropertyInfoValueGetter(object target, string name)
        {
            Dictionary<string, PropertyInfo> nameToProperties;
            var type = target.GetType();
            if (!_properties.TryGetValue(type, out nameToProperties))
            {
                nameToProperties = type.GetProperties()
                                       .Where(PropertyCanGetValue)
                                       .ToDictionary(key => key.Name, value => value);
                _properties.TryAdd(type, nameToProperties);
            }
            if (nameToProperties.ContainsKey(name))
            {
                return new PropertyInfoValueGetter(target, nameToProperties[name]);
            }

            return null;
        }

        private static bool PropertyCanGetValue(PropertyInfo property)
        {
            return property.CanRead;
        }

        private readonly object _target;
        private readonly PropertyInfo _propertyInfo;

        private PropertyInfoValueGetter(object target, PropertyInfo propertyInfo)
        {
            _target = target;
            _propertyInfo = propertyInfo;
        }

        public override object GetValue()
        {
            return _propertyInfo.GetValue(_target, null);
        }
    }

    internal class NoValueGetter : ValueGetter
    {
        public override object GetValue()
        {
            return NoValue;
        }
    }
}