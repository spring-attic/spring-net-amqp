using System.Collections;
using System.Collections.Generic;

using Moq;

using NUnit.Framework;

using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    public class DefaultTypeMapperTests 
    {
	    private DefaultTypeMapper typeMapper = new DefaultTypeMapper();
	    private  MessageProperties props = new MessageProperties();
	
        [SetUp]
        public void SetUp()
        {
            this.typeMapper = new DefaultTypeMapper();
            this.props = new MessageProperties();
        }

        [Test]
	    public void ShouldThrowAnExceptionWhenTypeIdNotPresent()
        {
		    try
            {
			    this.typeMapper.ToType(props);
		    }
            catch (MessageConversionException e) 
            {
			    var typeIdFieldName = this.typeMapper.TypeIdFieldName;
			    Assert.That(e.Message, Contains.Substring("Could not resolve " + typeIdFieldName + " in header"));
		    }
	    }
	
	    [Test]
	    public void ShouldLookInTheTypeIdFieldNameToFindTheTypeName()
        {
		    props.Headers.Add("__TypeId__", "System.String");
		    //Given(classMapper.TypeIdFieldName).willReturn("type");
	        
		    var type = this.typeMapper.ToType(props);
		
		    Assert.That(type, Is.EqualTo(typeof(string)));
	    }
	
        [Test]
        public void ShouldUseTheTypeProvidedByTheLookupMapIfPresent(){
            props.Headers.Add("__TypeId__", "trade");

            this.typeMapper.IdTypeMapping = new Hashtable() { { "trade", typeof(SimpleTrade).ToTypeName() } };
            
            var type = this.typeMapper.ToType(props);
		
            Assert.AreEqual(type, typeof(SimpleTrade));
        }

        [Test]
        public void TestIndexOfOccurencehWorksForOccurence1()
        {
            const string input = "foo<br />bar<br />baz<br />";
            Assert.AreEqual(3, input.IndexOfOccurence("<br />", 0, 1));
        }

        [Test]
        public void TestIndexOfOccurenceWorksForOccurenceh2()
        {
            const string input = "foo<br />whatthedeuce<br />kthxbai<br />";
            Assert.AreEqual(21, input.IndexOfOccurence("<br />", 0, 2));
        }

        [Test]
        public void TestIndexOfOccurenceWorksForOccurence3()
        {
            const string input = "foo<br />whatthedeuce<br />kthxbai<br />";
            Assert.AreEqual(34, input.IndexOfOccurence("<br />", 0, 3));
        }
	
        [Test]
        public void ShouldReturnDictionaryForFieldWithDictionary(){
            props.Headers.Add("__TypeId__", "Dictionary");

            var type = this.typeMapper.ToType(props);
		
            Assert.That(type, Is.EqualTo(typeof(Dictionary<string, object>)));
        }

        [Test]
        public void FromTypeShouldPopulateWithTypeNameByDefault()
        {
            this.typeMapper.FromType(typeof(SimpleTrade), props);

            var typeName = props.Headers[this.typeMapper.TypeIdFieldName];
            Assert.That(typeName, Is.EqualTo(typeof(SimpleTrade).FullName));
        }

        [Test]
        public void ShouldUseSpecialNameForTypeIfPresent()
        {
            typeMapper.IdTypeMapping = new Hashtable () { { "daytrade", typeof(SimpleTrade).ToTypeName() } };
            typeMapper.AfterPropertiesSet();
		
            typeMapper.FromType(typeof(SimpleTrade), props);
		
            var typeName = props.Headers[typeMapper.TypeIdFieldName];
            Assert.That(typeName, Is.EqualTo("daytrade"));
        }   
	
        [Test]
        public void ShouldConvertAnyHashtableToUseDictionaries()
        {
            typeMapper.FromType(typeof(Hashtable), props);

            var typeName = props.Headers[typeMapper.TypeIdFieldName];
		
            Assert.That(typeName, Is.EqualTo("Dictionary"));
        }

        // Doesn't make sense for .NET...
        //    private Map<String, Class<?>> map(String string, Class<?> class1) {
        //        Map<String, Class<?>> map = new HashMap<String, Class<?>>();
        //        map.put(string, class1);
        //        return map;
        //    }

}

}