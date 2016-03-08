﻿using Raven.MessageQueue;
using Raven.MessageQueue.WithRabbitMQ;
using Raven.Rpc.Tracing;
using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.TracingRecord
{

    public class RecordHandle : Handle
    {
        private static readonly string hostName = ConfigurationManager.AppSettings["RabbitMQHost"];
        private static readonly string username = "liangyi";
        private static readonly string password = "123456";

        #region GetInstance

        private static Lazy<RecordHandle> _instance = new Lazy<RecordHandle>(() => new RecordHandle("ItemCoupon"));

        public static RecordHandle GetInstance
        {
            get { return _instance.Value; }
        }

        #endregion
        
        protected override Action ProcessWorkAction
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Options rabbitMQOptions;
        RabbitMQClient rabbitMQClient;

        ServerRSLogsRep serverRSlogRep;
        ClientSRLogsRep clientSRlogRep;

        public RecordHandle(string serverName)
            : base(serverName, 5)
        {
            rabbitMQOptions = new Raven.MessageQueue.WithRabbitMQ.Options()
            {
                SerializerType = SerializerType.NewtonsoftJson,
                HostName = hostName,
                Password = password,
                UserName = username,
                //MaxQueueCount = 100000,
                Loger = new Loger()
            };
            rabbitMQClient = RabbitMQClient.GetInstance(rabbitMQOptions);


            serverRSlogRep = new ServerRSLogsRep();
            clientSRlogRep = new ClientSRLogsRep();
        }

        /// <summary>
        /// 调用方法
        /// </summary>
        public void ProcessResetAwardData()
        {

            try
            {
                var list = rabbitMQClient.ReceiveBatch<ServerRSLogs>(Config.TrackServerRSQueueName);

                if (list != null && list.Count > 0)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var l = list[i];
                        //List<string> jsonObjectKey = new List<string>();
                        if (l.Extension != null)
                        {
                            foreach (var kv in l.Extension)
                            {
                                if (kv.Value.GetType().FullName == "Newtonsoft.Json.Linq.JObject")//"Jil.DeserializeDynamic.JsonObject")
                                {
                                    var str = Newtonsoft.Json.JsonConvert.SerializeObject(kv.Value);
                                    l.Extensions.Add(kv.Key, MongoDB.Bson.BsonDocument.Parse(str));
                                }
                                else
                                {
                                    l.Extensions.Add(kv.Key, MongoDB.Bson.BsonValue.Create(kv.Value));
                                }
                            }
                            l.Extension = null;
                            //if (jsonObjectKey.Count > 0)
                            //{
                            //    foreach (var k in jsonObjectKey)
                            //    {
                            //        var value = l.Extension[k];
                            //        var str = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                            //        l.Extension[k] = MongoDB.Bson.BsonDocument.Parse(str);
                            //    }
                            //}
                        }
                    }
                    serverRSlogRep.InsertBatch(list);
                }

                var list2 = rabbitMQClient.ReceiveBatch<ClientSRLogs>(Config.TrackClientSRQueueName);

                if (list2 != null && list2.Count > 0)
                {
                    for (var i = 0; i < list2.Count; i++)
                    {
                        var l = list2[i];
                        //List<string> jsonObjectKey = new List<string>();
                        if (l.Extension != null)
                        {
                            foreach (var kv in l.Extension)
                            {
                                if (kv.Value.GetType().FullName == "Newtonsoft.Json.Linq.JObject")//"Jil.DeserializeDynamic.JsonObject")
                                {
                                    var str = Newtonsoft.Json.JsonConvert.SerializeObject(kv.Value);
                                    l.Extensions.Add(kv.Key, MongoDB.Bson.BsonDocument.Parse(str));
                                }
                                else
                                {
                                    l.Extensions.Add(kv.Key, MongoDB.Bson.BsonValue.Create(kv.Value));
                                }
                            }
                            l.Extension = null;
                            //if (jsonObjectKey.Count > 0)
                            //{
                            //    foreach (var k in jsonObjectKey)
                            //    {
                            //        var value = l.Extension[k];
                            //        var str = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                            //        l.Extension[k] = MongoDB.Bson.BsonDocument.Parse(str);
                            //    }
                            //}
                        }
                    }
                    clientSRlogRep.InsertBatch(list2);
                }
            }
            catch (Exception ex)
            {
                Loger.GetInstance.LogError(ex, null);
            }
        }
    }
}