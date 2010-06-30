#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Support;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    [TestFixture]
    public class CachingConnectionFactoryTests
    {
       
        [Test]
        public void CachedChannel()
        {

            TestConnectionFactory testConnectionFactory = new TestConnectionFactory();
          
            CachingConnectionFactory cachingConnectionFactory = new CachingConnectionFactory(testConnectionFactory, "localhost:" + RabbitUtils.DEFAULT_PORT);
            
            IConnection con1 = cachingConnectionFactory.CreateConnection();
            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);

            IModel model1 = con1.CreateModel();
            TestModel testModel = GetTestModel(model1);
            Assert.AreEqual(1, testModel.CreatedCount);
            Assert.AreEqual(0, testModel.CloseCount);
            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);

            model1.Close();  // won't close, will put in channel cache.
            Assert.AreEqual(0, testModel.CloseCount);

            IModel model2 = con1.CreateModel();
            TestModel testModel2 = GetTestModel(model2);


            Assert.AreSame(testModel, testModel2);

            Assert.AreEqual(1, testModel.CreatedCount);
            Assert.AreEqual(0, testModel.CloseCount);
            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);
        }

        [Test]
        public void CachedModelTwoRequests()
        {
            TestConnectionFactory testConnectionFactory = new TestConnectionFactory();

            CachingConnectionFactory cachingConnectionFactory = new CachingConnectionFactory(testConnectionFactory, "localhost:" + RabbitUtils.DEFAULT_PORT);

            //Create a session
            IConnection con1 = cachingConnectionFactory.CreateConnection();
            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);

            //Create a model
            IModel model1 = con1.CreateModel();
            TestModel testModel1 = GetTestModel(model1);
            Assert.AreEqual(1, testModel1.CreatedCount);
            Assert.AreEqual(0, testModel1.CloseCount);


            //will create a new model, not taken from the cache since cache size is 1.
            IModel model2 = con1.CreateModel();
            TestModel testModel2 = GetTestModel(model2);
            Assert.AreEqual(1, testModel2.CreatedCount);
            Assert.AreEqual(0, testModel2.CloseCount);


            Assert.AreNotSame(testModel1, testModel2);
            Assert.AreNotSame(model1, model2);

            model1.Close(); // will put the model in the cache

            // now get a new model, will be taken from the cache
            IModel model3 = con1.CreateModel();
            TestModel testModel3 = GetTestModel(model3);
            Assert.AreSame(testModel1, testModel3);
            Assert.AreSame(model1, model3);
            Assert.AreEqual(1, testModel1.CreatedCount);
            Assert.AreEqual(0, testModel1.CloseCount);

            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);

            
        }


        [Test]
        public void CachedModelTwoRequestsWithCacheSize()
        {
            TestConnectionFactory testConnectionFactory = new TestConnectionFactory();

            CachingConnectionFactory cachingConnectionFactory = new CachingConnectionFactory(testConnectionFactory, "localhost:" + RabbitUtils.DEFAULT_PORT);
            cachingConnectionFactory.ChannelCacheSize = 2;
            cachingConnectionFactory.AfterPropertiesSet();            

            //Create a session
            IConnection con1 = cachingConnectionFactory.CreateConnection();
            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);

            IModel model1 = con1.CreateModel();
            IModel model2 = con1.CreateModel();

            model1.Close(); // should be ignored, and add last into model cache.
            model2.Close(); // should be ignored, and add last into model cache.

            IModel m1 = con1.CreateModel();
            IModel m2 = con1.CreateModel();

            Assert.AreNotSame(m1, m2);
            Assert.AreSame(m1, model1);
            Assert.AreSame(m2, model2);

            m1.Close();
            m2.Close();

            
            Assert.AreEqual(2, GetTestConnection(con1).CreateModelCount);

            con1.Close();  // should be ignored.

            Assert.AreEqual(0, GetTestConnection(con1).CloseCount);
            Assert.AreEqual(0, GetTestModel(model1).CloseCount);
            Assert.AreEqual(0, GetTestModel(model2).CloseCount);

            Assert.AreEqual(1, testConnectionFactory.CreateConnectionCount);


        }

        private TestConnection GetTestConnection(IConnection connection)
        {
            IConnection con = ConnectionFactoryUtils.GetTargetConnection(connection);
            TestConnection testConnection = con as TestConnection;
            Assert.IsNotNull(testConnection);
            return testConnection;
        }

        private static TestModel GetTestModel(IModel model)
        {
            IModel m1 = ConnectionFactoryUtils.GetTargetModel(model);
            TestModel testModel = m1 as TestModel;
            Assert.IsNotNull(testModel);
            return testModel;
        }
    }
}