// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultTypeMapperTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Messaging.Amqp.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    /// <summary>The default type mapper tests.</summary>
    /// <author>James Carr</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class DefaultTypeMapperTests
    {
        private DefaultTypeMapper typeMapper = new DefaultTypeMapper();
        private MessageProperties props = new MessageProperties();

        /// <summary>The set up.</summary>
        [SetUp]
        public void SetUp()
        {
            this.typeMapper = new DefaultTypeMapper();
            this.props = new MessageProperties();
        }

        /// <summary>The should throw an exception when type id not present.</summary>
        [Test]
        public void ShouldThrowAnExceptionWhenTypeIdNotPresent()
        {
            var exceptionWasThrown = false;
            try
            {
                this.typeMapper.ToType(this.props);
                Assert.Fail("Exception should have been thrown.");
            }
            catch (MessageConversionException e)
            {
                exceptionWasThrown = true;
                var typeIdFieldName = this.typeMapper.TypeIdFieldName;
                Assert.That(e.Message, Contains.Substring("Could not resolve " + typeIdFieldName + " in header"));
            }

            if (!exceptionWasThrown)
            {
                Assert.Fail("Exception should have been thrown.");
            }
        }

        /// <summary>The should look in the type id field name to find the type name.</summary>
        [Test]
        public void ShouldLookInTheTypeIdFieldNameToFindTheTypeName()
        {
            var typeMapper = new Mock<DefaultTypeMapper>();
            typeMapper.Setup(m => m.TypeIdFieldName).Returns("type");
            this.props.Headers.Add("type", "System.String");

            var type = typeMapper.Object.ToType(this.props);

            Assert.That(type, Is.EqualTo(typeof(string)));
        }

        /// <summary>The should use the type provided by the lookup map if present.</summary>
        [Test]
        public void ShouldUseTheTypeProvidedByTheLookupMapIfPresent()
        {
            this.props.Headers.Add("__TypeId__", "trade");

            this.typeMapper.IdTypeMapping = new Dictionary<string, object> { { "trade", typeof(SimpleTrade) } };

            var type = this.typeMapper.ToType(this.props);

            Assert.AreEqual(type, typeof(SimpleTrade));
        }

        /// <summary>The test index of occurenceh works for occurence 1.</summary>
        [Test]
        public void TestIndexOfOccurencehWorksForOccurence1()
        {
            const string input = "foo<br />bar<br />baz<br />";
            Assert.AreEqual(3, input.IndexOfOccurence("<br />", 0, 1));
        }

        /// <summary>The test index of occurence works for occurenceh 2.</summary>
        [Test]
        public void TestIndexOfOccurenceWorksForOccurenceh2()
        {
            const string input = "foo<br />whatthedeuce<br />kthxbai<br />";
            Assert.AreEqual(21, input.IndexOfOccurence("<br />", 0, 2));
        }

        /// <summary>The test index of occurence works for occurence 3.</summary>
        [Test]
        public void TestIndexOfOccurenceWorksForOccurence3()
        {
            const string input = "foo<br />whatthedeuce<br />kthxbai<br />";
            Assert.AreEqual(34, input.IndexOfOccurence("<br />", 0, 3));
        }

        /// <summary>The should return dictionary for field with dictionary.</summary>
        [Test]
        public void ShouldReturnDictionaryForFieldWithDictionary()
        {
            this.props.Headers.Add("__TypeId__", "Dictionary");

            var type = this.typeMapper.ToType(this.props);

            Assert.That(type, Is.EqualTo(typeof(Dictionary<string, object>)));
        }

        /// <summary>The from type should populate with type name by default.</summary>
        [Test]
        public void FromTypeShouldPopulateWithTypeNameByDefault()
        {
            this.typeMapper.FromType(typeof(SimpleTrade), this.props);

            var typeName = this.props.Headers[this.typeMapper.TypeIdFieldName];
            Assert.That(typeName, Is.EqualTo(typeof(SimpleTrade).FullName));
        }

        /// <summary>The should use special name for type if present.</summary>
        [Test]
        public void ShouldUseSpecialNameForTypeIfPresent()
        {
            this.typeMapper.IdTypeMapping = new Dictionary<string, object> { { "daytrade", typeof(SimpleTrade) } };
            this.typeMapper.AfterPropertiesSet();

            this.typeMapper.FromType(typeof(SimpleTrade), this.props);

            var typeName = this.props.Headers[this.typeMapper.TypeIdFieldName];
            Assert.That(typeName, Is.EqualTo("daytrade"));
        }

        /// <summary>The should convert any hashtable to use dictionaries.</summary>
        [Test]
        public void ShouldConvertAnyHashtableToUseDictionaries()
        {
            this.typeMapper.FromType(typeof(Hashtable), this.props);

            var typeName = this.props.Headers[this.typeMapper.TypeIdFieldName];

            Assert.That(typeName, Is.EqualTo("Dictionary"));
        }

        // Doesn't make sense for .NET...
        // private Map<String, Class<?>> map(String string, Class<?> class1) {
        // Map<String, Class<?>> map = new HashMap<String, Class<?>>();
        // map.put(string, class1);
        // return map;
        // }
    }
}
