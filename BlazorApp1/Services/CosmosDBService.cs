using Azure.Core;
using Azure.Identity;
using BlazorAIChat.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Container = Microsoft.Azure.Cosmos.Container;

namespace BlazorAIChat.Services
{
    public class CosmosDbService
    {

        private readonly Container _chatContainer;

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        /// <param name="endpoint">Endpoint URI.</param>
        /// <param name="key">Auth key for Cosmos DB</param>
        /// <param name="databaseName">Name of the database to access.</param>
        /// <param name="chatContainerName">Name of the chat container to access.</param>
        /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, or chatContainerName is either null or empty.</exception>
        /// <remarks>
        /// This constructor will validate credentials and create a service client instance.
        /// </remarks>
        public CosmosDbService(IConfiguration config)
        {
            var connectionString = config["ConnectionStrings:CosmosDb"];
            var databaseName = config["CosmosDb:Database"];
            var chatContainerName = config["CosmosDb:Container"];

            ArgumentNullException.ThrowIfNullOrEmpty(connectionString);
            ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
            ArgumentNullException.ThrowIfNullOrEmpty(chatContainerName);

            CosmosSerializationOptions options = new()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            };

            CosmosClient client = new CosmosClientBuilder(connectionString)
                .WithSerializerOptions(options)
                .Build();


            Database database = client.GetDatabase(databaseName)!;
            Container chatContainer = database.CreateContainerIfNotExistsAsync(
                id: chatContainerName,
                partitionKeyPath: "/sessionId"
            ).GetAwaiter().GetResult();

            _chatContainer = chatContainer ??
                throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");

        }

        /// <summary>
        /// Creates a new chat session.
        /// </summary>
        /// <param name="session">Chat session item to create.</param>
        /// <returns>Newly created chat session item.</returns>
        public async Task<Session> InsertSessionAsync(Session session)
        {
            PartitionKey partitionKey = new(session.SessionId);
            return await _chatContainer.CreateItemAsync<Session>(
                item: session,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Creates a new chat message.
        /// </summary>
        /// <param name="message">Chat message item to create.</param>
        /// <returns>Newly created chat message item.</returns>
        public async Task<Message> InsertMessageAsync(Message message)
        {
            PartitionKey partitionKey = new(message.SessionId);
            Message newMessage = message with { TimeStamp = DateTime.UtcNow };
            return await _chatContainer.CreateItemAsync<Message>(
                item: message,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Gets a list of all current chat sessions.
        /// </summary>
        /// <returns>List of distinct chat session items.</returns>
        public async Task<List<Session>> GetSessionsAsync(string userId)
        {
            QueryDefinition query = new QueryDefinition($"SELECT DISTINCT * FROM c WHERE c.type = @type and c.userId=@userId")
                .WithParameter("@type", nameof(Session))
                .WithParameter("@userId", userId);

            FeedIterator<Session> response = _chatContainer.GetItemQueryIterator<Session>(query);

            List<Session> output = new();
            while (response.HasMoreResults)
            {
                FeedResponse<Session> results = await response.ReadNextAsync();
                output.AddRange(results);
            }
            return output;
        }

        /// <summary>
        /// Gets a list of all current chat messages for a specified session identifier.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
        /// <returns>List of chat message items for the specified session.</returns>
        public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.sessionId = @sessionId AND c.type = @type")
                .WithParameter("@sessionId", sessionId)
                .WithParameter("@type", nameof(Message));

            FeedIterator<Message> results = _chatContainer.GetItemQueryIterator<Message>(query);

            List<Message> output = new();
            while (results.HasMoreResults)
            {
                FeedResponse<Message> response = await results.ReadNextAsync();
                output.AddRange(response);
            }
            return output;
        }

        /// <summary>
        /// Updates an existing chat session.
        /// </summary>
        /// <param name="session">Chat session item to update.</param>
        /// <returns>Revised created chat session item.</returns>
        public async Task<Session> UpdateSessionAsync(Session session)
        {
            PartitionKey partitionKey = new(session.SessionId);
            return await _chatContainer.ReplaceItemAsync(
                item: session,
                id: session.Id,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Returns an existing chat session.
        /// </summary>
        /// <param name="sessionId">Chat session id for the session to return.</param>
        /// <returns>Chat session item.</returns>
        public async Task<Session> GetSessionAsync(string sessionId)
        {
            ItemResponse<Session>? results = null;
            try
            {
                PartitionKey partitionKey = new(sessionId);
                results = await _chatContainer.ReadItemAsync<Session>(
                    partitionKey: partitionKey,
                    id: sessionId
                    );
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Batch create chat message and update session.
        /// </summary>
        /// <param name="messages">Chat message and session items to create or replace.</param>
        public async Task UpsertSessionBatchAsync(params dynamic[] messages)
        {

            //Make sure items are all in the same partition
            if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
            {
                throw new ArgumentException("All items must have the same partition key.");
            }

            PartitionKey partitionKey = new(messages[0].SessionId);
            TransactionalBatch batch = _chatContainer.CreateTransactionalBatch(partitionKey);

            foreach (var message in messages)
            {
                batch.UpsertItem(item: message);
            }

            await batch.ExecuteAsync();
        }

        /// <summary>
        /// Batch deletes an existing chat session and all related messages.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
        public async Task DeleteSessionAndMessagesAsync(string sessionId)
        {
            PartitionKey partitionKey = new(sessionId);

            QueryDefinition query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.sessionId = @sessionId")
                    .WithParameter("@sessionId", sessionId);

            FeedIterator<string> response = _chatContainer.GetItemQueryIterator<string>(query);

            TransactionalBatch batch = _chatContainer.CreateTransactionalBatch(partitionKey);
            while (response.HasMoreResults)
            {
                FeedResponse<string> results = await response.ReadNextAsync();
                foreach (var itemId in results)
                {
                    batch.DeleteItem(
                        id: itemId
                    );
                }
            }
            await batch.ExecuteAsync();
        }
    }
}
