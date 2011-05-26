using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Logging;
using NUnit.Framework;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Test
{

    /**
     * <p>
     * ReflectionTestUtils is a collection of reflection-based utility methods for
     * use in unit and integration testing scenarios.
     * </p>
     * <p>
     * There are often situations in which it would be beneficial to be able to set
     * a non-<code>public</code> field or invoke a non-<code>public</code> setter
     * method when testing code involving, for example:
     * </p>
     * <ul>
     * <li>ORM frameworks such as JPA and Hibernate which condone the usage of
     * <code>private</code> or <code>protected</code> field access as opposed to
     * <code>public</code> setter methods for properties in a domain entity.</li>
     * <li>Spring's support for annotations such as
     * {@link org.springframework.beans.factory.annotation.Autowired @Autowired} and
     * {@link javax.annotation.Resource @Resource} which provides dependency
     * injection for <code>private</code> or <code>protected</code> fields, setter
     * methods, and configuration methods.</li>
     * </ul>
     * 
     * @author Sam Brannen
     * @author Juergen Hoeller
     * @since 2.5
     * @see ReflectionUtils
     */
    public class ReflectionTestUtils
    {
        private static readonly String SETTER_PREFIX = "set";

        private static readonly String GETTER_PREFIX = "get";

        private static readonly ILog logger = LogManager.GetLogger(typeof(ReflectionTestUtils));


        /**
         * Set the {@link Field field} with the given <code>name</code> on the
         * provided {@link Object target object} to the supplied <code>value</code>.
         * <p>
         * This method traverses the class hierarchy in search of the desired field.
         * In addition, an attempt will be made to make non-<code>public</code>
         * fields <em>accessible</em>, thus allowing one to set
         * <code>protected</code>, <code>private</code>, and
         * <em>package-private</em> fields.
         * 
         * @param target the target object on which to set the field
         * @param name the name of the field to set
         * @param value the value to set
         * @see ReflectionUtils#findField(Class, String, Class)
         * @see ReflectionUtils#makeAccessible(Field)
         * @see ReflectionUtils#setField(Field, Object, Object)
         */
        public static void SetField(object target, string name, object value)
        {
            SetField(target, name, value, null);
        }

        /**
         * Set the {@link Field field} with the given <code>name</code> on the
         * provided {@link Object target object} to the supplied <code>value</code>.
         * <p>
         * This method traverses the class hierarchy in search of the desired field.
         * In addition, an attempt will be made to make non-<code>public</code>
         * fields <em>accessible</em>, thus allowing one to set
         * <code>protected</code>, <code>private</code>, and
         * <em>package-private</em> fields.
         * 
         * @param target the target object on which to set the field
         * @param name the name of the field to set
         * @param value the value to set
         * @param type the type of the field (may be <code>null</code>)
         * @see ReflectionUtils#findField(Class, String, Class)
         * @see ReflectionUtils#makeAccessible(Field)
         * @see ReflectionUtils#setField(Field, Object, Object)
         */
        public static void SetField(object target, string name, object value, Type type)
        {
            Assert.NotNull(target, "Target object must not be null");
            Field field = ReflectionUtils.findField(target.getClass(), name, type);
            if (field == null)
            {
                throw new IllegalArgumentException("Could not find field [" + name + "] on target [" + target + "]");
            }

            if (logger.IsDebugEnabled())
            {
                logger.Debug("Setting field [" + name + "] on target [" + target + "]");
            }
            ReflectionUtils.MakeAccessible(field);
            ReflectionUtils.SetField(field, target, value);
        }

        /**
         * Get the field with the given <code>name</code> from the provided target
         * object.
         * <p>
         * This method traverses the class hierarchy in search of the desired field.
         * In addition, an attempt will be made to make non-<code>public</code>
         * fields <em>accessible</em>, thus allowing one to get
         * <code>protected</code>, <code>private</code>, and
         * <em>package-private</em> fields.
         * 
         * @param target the target object on which to set the field
         * @param name the name of the field to get
         * @return the field's current value
         * @see ReflectionUtils#findField(Class, String, Class)
         * @see ReflectionUtils#makeAccessible(Field)
         * @see ReflectionUtils#setField(Field, Object, Object)
         */
        public static object GetField(object target, string name)
        {
            Assert.NotNull(target, "Target object must not be null");
            Field field = ReflectionUtils.findField(target.getClass(), name);
            if (field == null)
            {
                throw new ArgumentException("Could not find field [" + name + "] on target [" + target + "]");
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug("Getting field [" + name + "] from target [" + target + "]");
            }
           // ReflectionUtils.MakeAccessible(field);
            return ReflectionUtils.GetInstanceFieldValue(target, namefield, target);
        }

        /**
         * Invoke the setter method with the given <code>name</code> on the supplied
         * target object with the supplied <code>value</code>.
         * <p>
         * This method traverses the class hierarchy in search of the desired
         * method. In addition, an attempt will be made to make non-
         * <code>public</code> methods <em>accessible</em>, thus allowing one to
         * invoke <code>protected</code>, <code>private</code>, and
         * <em>package-private</em> setter methods.
         * <p>
         * In addition, this method supports JavaBean-style <em>property</em> names.
         * For example, if you wish to set the <code>name</code> property on the
         * target object, you may pass either &quot;name&quot; or
         * &quot;setName&quot; as the method name.
         * 
         * @param target the target object on which to invoke the specified setter
         * method
         * @param name the name of the setter method to invoke or the corresponding
         * property name
         * @param value the value to provide to the setter method
         * @see ReflectionUtils#findMethod(Class, String, Class[])
         * @see ReflectionUtils#makeAccessible(Method)
         * @see ReflectionUtils#invokeMethod(Method, Object, Object[])
         */
        public static void InvokeSetterMethod(object target, string name, object value)
        {
            InvokeSetterMethod(target, name, value, null);
        }

        /**
         * Invoke the setter method with the given <code>name</code> on the supplied
         * target object with the supplied <code>value</code>.
         * <p>
         * This method traverses the class hierarchy in search of the desired
         * method. In addition, an attempt will be made to make non-
         * <code>public</code> methods <em>accessible</em>, thus allowing one to
         * invoke <code>protected</code>, <code>private</code>, and
         * <em>package-private</em> setter methods.
         * <p>
         * In addition, this method supports JavaBean-style <em>property</em> names.
         * For example, if you wish to set the <code>name</code> property on the
         * target object, you may pass either &quot;name&quot; or
         * &quot;setName&quot; as the method name.
         * 
         * @param target the target object on which to invoke the specified setter
         * method
         * @param name the name of the setter method to invoke or the corresponding
         * property name
         * @param value the value to provide to the setter method
         * @param type the formal parameter type declared by the setter method
         * @see ReflectionUtils#findMethod(Class, String, Class[])
         * @see ReflectionUtils#makeAccessible(Method)
         * @see ReflectionUtils#invokeMethod(Method, Object, Object[])
         */
        public static void InvokeSetterMethod(object target, string name, object value, Type type) {
		Assert.NotNull(target, "Target object must not be null");
		Assert.NotNull(name, "Method name must not be empty");
		Type[] paramTypes = (type != null ? new Type[] { type } : null);

		String setterMethodName = name;
		if (!name.StartsWith(SETTER_PREFIX)) {
			setterMethodName = SETTER_PREFIX + StringUtils.Capitalize(name);
		}
		var method = ReflectionUtils.GetMethod(target.GetType(), setterMethodName, paramTypes));
		if (method == null && !setterMethodName.Equals(name)) {
			setterMethodName = name;
			method = ReflectionUtils.GetMethod(target.GetType(), setterMethodName, paramTypes);
		}
		if (method == null) {
			throw new ArgumentException("Could not find setter method [" + setterMethodName + "] on target ["
					+ target + "] with parameter type [" + type + "]");
		}

		if (logger.IsDebugEnabled) {
			logger.Debug("Invoking setter method [" + setterMethodName + "] on target [" + target + "]");
		}
		//ReflectionUtils.makeAccessible(method);
		method.Invoke(target, new object[] { value });
	}

        /**
         * Invoke the getter method with the given <code>name</code> on the supplied
         * target object with the supplied <code>value</code>.
         * <p>
         * This method traverses the class hierarchy in search of the desired
         * method. In addition, an attempt will be made to make non-
         * <code>public</code> methods <em>accessible</em>, thus allowing one to
         * invoke <code>protected</code>, <code>private</code>, and
         * <em>package-private</em> getter methods.
         * <p>
         * In addition, this method supports JavaBean-style <em>property</em> names.
         * For example, if you wish to get the <code>name</code> property on the
         * target object, you may pass either &quot;name&quot; or
         * &quot;getName&quot; as the method name.
         * 
         * @param target the target object on which to invoke the specified getter
         * method
         * @param name the name of the getter method to invoke or the corresponding
         * property name
         * @return the value returned from the invocation
         * @see ReflectionUtils#findMethod(Class, String, Class[])
         * @see ReflectionUtils#makeAccessible(Method)
         * @see ReflectionUtils#invokeMethod(Method, Object, Object[])
         */
        public static object InvokeGetterMethod(Object target, String name)
        {
            Assert.NotNull(target, "Target object must not be null");
            Assert.NotNull(name, "Method name must not be empty");

            String getterMethodName = name;
            if (!name.StartsWith(GETTER_PREFIX))
            {
                getterMethodName = GETTER_PREFIX + StringUtils.capitalize(name);
            }
            var method = ReflectionUtils.GetMethod(target.GetType(), getterMethodName, null);
            if (method == null && !getterMethodName.Equals(name))
            {
                getterMethodName = name;
                method = ReflectionUtils.GetMethod(target.GetType(), getterMethodName, null);
            }
            if (method == null)
            {
                throw new ArgumentException("Could not find getter method [" + getterMethodName + "] on target ["
                        + target + "]");
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug("Invoking getter method [" + getterMethodName + "] on target [" + target + "]");
            }
            //ReflectionUtils.makeAccessible(method);
            return method.Invoke(target, null);
        }

    }
}
