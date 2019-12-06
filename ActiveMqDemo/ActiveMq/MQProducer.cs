using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Newtonsoft.Json;

namespace ActiveMqDemo.ActiveMq
{
    /// <summary>
    /// 提供者
    /// </summary>
    public class MQProducer
    {
        // = "failover(tcp://localhost:616161)"
        private static string MqUri;
        private static IConnectionFactory factory;

        /// <summary>
        /// 初始化提供者
        /// </summary>
        /// <param name="mqUri"></param>
        public static void InitProducer(string mqUri)
        {
            MqUri = mqUri;
            factory = new ConnectionFactory(MqUri);
        }

        /// <summary>
        /// 发送Queue队列消息   点对点
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="msg"></param>
        public static void SendQueueMessage(string queueName,string msg)
        {
            if(factory == null)
            {
                return;
            }

            //创建链接 Connection   
            using (var connection = factory.CreateConnection())
            {
                connection.Start();
                //创建Session
                using (var session = connection.CreateSession())
                {
                    //创建提供者Producer
                    //using (var producer = session.CreateProducer(session.GetDestination(queueName, DestinationType.Queue)))
                    IDestination destination = new ActiveMQQueue(queueName);
                    using (var producer = session.CreateProducer(destination))
                    {
                        //持久化模式
                        producer.DeliveryMode = MsgDeliveryMode.Persistent;
                        //详细优先级
                        producer.Priority = MsgPriority.Normal;
                        //消息生存时间
                        producer.TimeToLive = TimeSpan.FromHours(1);
                        //发送消息
                        producer.Send(session.CreateTextMessage(msg));
                    }
                }
            }
        }

        /// <summary>
        /// 发送Topic消息  发布/订阅
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="msg"></param>
        public static void SendTopicMessage(string topicName, string msg)
        {
            if (factory == null)
            {
                return;
            }

            //创建连接Connection
            using (var connection = factory.CreateConnection())
            {
                connection.Start();
                //创建Session
                using (var session = connection.CreateSession())
                {
                    //创建提供者Producer
                    IDestination destination = session.GetTopic(topicName);
                    using (var producer = session.CreateProducer(destination))
                    {
                        //持久化模式
                        producer.DeliveryMode = MsgDeliveryMode.Persistent;
                        //优先级
                        producer.Priority = MsgPriority.Normal;
                        //消息生存时间
                        producer.TimeToLive = TimeSpan.FromMinutes(30);
                        //发送Topic消息
                        producer.Send(session.CreateTextMessage(msg));
                    }
                }
            }
        }

        /// <summary>
        /// 发送序列化对象信息  Queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        public static void SendQueueMessage<T>(string queueName, T message)
        {
            var msg = JsonConvert.SerializeObject(message);
            SendQueueMessage(queueName, msg);
        }

        /// <summary>
        /// 发送序列化对象信息  Topic
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topicName"></param>
        /// <param name="message"></param>
        public static void SendTopicMessage<T>(string topicName, T message)
        {
            var msg = JsonConvert.SerializeObject(message);
            SendTopicMessage(topicName, msg);
        }
    }
}
