using System;
using System.Text;
using System.Text.Json;
using Authentication.DAL.Dtos;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Authentication.API.AsyncDataService
{
    public interface IMessageBusClient
    {
        void PublishNewUser(BaseUserPublishDto baseUserPublishDto);
    }
}
