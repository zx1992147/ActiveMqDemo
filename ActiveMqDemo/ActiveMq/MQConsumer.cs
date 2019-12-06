using System;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;

namespace ActiveMqDemo.ActiveMq
{
    /// <summary>
    /// 消费者
    /// </summary>
    public class MQConsumer
    {
        /// <summary>
        /// MQ地址
        /// </summary>
        private string MqUri;
        /// <summary>
        /// 创建连接工厂
        /// </summary>
        private IConnectionFactory factory;
        /// <summary>
        /// 连接
        /// </summary>
        private IConnection connection = null;

        /// <summary>
        /// 会话
        /// </summary>
        private ISession session = null;

        /// <summary>
        /// 是否活动
        /// </summary>
        private volatile bool isAlive = false;

        /// <summary>
        /// 消费者
        /// </summary>
        private IMessageConsumer consumer = null;

        /// <summary>
        /// 锁
        /// </summary>
        private object locker = new object();

        public bool initConsumer(string mquri,string topicOrQueueName,bool isTopic = true)
        {
            try
            {
                MqUri = mquri;

                if(factory == null)
                {
                    factory = new ConnectionFactory(MqUri);
                }

                if(connection != null)
                {
                    CloseConsumer();
                }

                //开始初始化MQConsumer
                //通过工厂构建连接
                connection = factory.CreateConnection();
                //添加连接异常监听处理事件
                connection.ExceptionListener += connection_ExceptionListener;
                //添加连接断开监听处理事件
                connection.ConnectionInterruptedListener += connection_ConnectionInterruptedListener;
                //添加连接恢复监听处理事件
                connection.ConnectionResumedListener += connection_ConnectionResumedListener;
                
                //设置超时事件
                connection.RequestTimeout = new TimeSpan(0, 0, 20);
                //会话时的应答模式 
                connection.AcknowledgementMode = AcknowledgementMode.AutoAcknowledge;
                //连接客户端ID  建议每次初始化就创建最新的
                connection.ClientId = Guid.NewGuid().ToString("N");

                //创建自动确认消息的会话
                //Session.Auto_ACKNOWLEDGE。当客户成功地从receive方法返回的时候，或者从MessageListener.onMessage方法成功返回的时候，会话自动确认客户收到的消息。
                //Session.CLIENT_ACKNOWLEDGE。客户通过消息的acknowledge方法确认消息。需要注意的是，在这种模式中，确认是在会话层进行：确认一个被消费的消息将自动确认所有已被会话消费的消息。例如，如果一个消息消费者消费了10个消息，然后确认第5个消息，那个所有10个消息都被确认。
                //Session.DUPS_ACKNOWLEDGE。在会话迟钝等情况下确认消息。如果JMS provider失败，那么可能会导致一些重复的消息。如果是重复的消息，那么JMS provider必须把消息头的JMSRecelivered字段设置为true。
                session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                //超时时间
                session.RequestTimeout = new TimeSpan(0, 0, 20);

                //创建消费者
                IDestination destination = isTopic ? (IDestination)session.GetTopic(topicOrQueueName) : session.GetQueue(topicOrQueueName);
                consumer = session.CreateConsumer(destination);
                //注册监听事件
                consumer.Listener += consumer_Listener;
                if (!connection.IsStarted)
                {
                    //启动连接
                    connection.Start();
                }

                isAlive = true;
                return true;
            }
            catch
            {

            }
            return true;

        }

        /// <summary>
        /// 消费者监听
        /// 经测试该函数是在前一个回调完成之后执行的,即回调是单线程执行的,需要自己实现多线程
        /// </summary>
        /// <param name="message"></param>
        private void consumer_Listener(IMessage message)
        {
            isAlive = true;
            try
            {
                #region 获取消息
                ITextMessage txtMsg = message as ITextMessage;

                if (txtMsg == null || string.IsNullOrWhiteSpace(txtMsg.Text))
                {
                    //NLogHelper.Warn("接收到空消息");
                    return;
                }
                #endregion

                string msg = txtMsg.Text.Trim();
                //NLogHelper.Info("接收到消息：" + msg);
                //MqConsumerEvent?.Invoke(true, msg);
            }
            catch (Exception ex)
            {
                //NLogHelper.Error("解析处理MQ消息失败：" + ex);
                //MqConsumerEvent?.Invoke(false, "解析处理MQ消息失败：" + ex);
            }
        }

        /// <summary>
        /// 连接异常
        /// </summary>
        /// <param name="exception"></param>
        public void connection_ExceptionListener(Exception exception)
        {
            isAlive = false;
            //记录异常日志
        }

        /// <summary>
        /// 连接断开
        /// </summary>
        public void connection_ConnectionInterruptedListener()
        {
            isAlive = false;
            //记录断开日志
        }

        /// <summary>
        /// 重连
        /// </summary>
        public void connection_ConnectionResumedListener()
        {
            isAlive = false;
            //记录重连日志
        }

        /// <summary>
        /// 关闭消费者
        /// </summary>
        public void CloseConsumer()
        {
            try
            {
                if(consumer != null)
                {
                    consumer.Close();
                    consumer.Dispose();
                }
                consumer = null;

                if(session != null)
                {
                    session.Close();
                    session.Dispose();
                }
                session = null;
            }
            catch
            {

            }

            try
            {
                if (connection != null)
                {
                    connection.Stop();
                    connection.Close();
                    connection.Dispose();
                }
                connection = null;
            }
            catch
            {

            }

            isAlive = false;
        }

        /// <summary>
        /// MQ是否存活
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            lock (locker)
            {
                return isAlive;
            }
        }
    }
}
