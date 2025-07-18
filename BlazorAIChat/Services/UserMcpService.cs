#pragma warning disable SKEXP0010, SKEXP0001, SKEXP0020, KMEXP00
using BlazorAIChat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorAIChat.Services
{
    public class UserMcpService
    {
        private readonly AIChatDBContext _dbContext;
        private readonly AppSettings _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UserMcpService> _logger;
        private static readonly string EncryptionKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("BlazorAIChatMcpKey")); // In production, use secure key management

        public UserMcpService(AIChatDBContext dbContext, IOptions<AppSettings> appSettings, IHttpClientFactory httpClientFactory, ILogger<UserMcpService> logger)
        {
            _dbContext = dbContext;
            _appSettings = appSettings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates MCP plugins for a specific user
        /// </summary>
        public async Task<List<KernelPlugin>> GetUserMcpPluginsAsync(string userId)
        {
            var plugins = new List<KernelPlugin>();

            if (_appSettings.Mcp?.Servers == null)
                return plugins;

            // Get user's MCP parameters
            var userParameters = await GetUserMcpParametersAsync(userId);

            foreach (var serverEntry in _appSettings.Mcp.Servers)
            {
                var serverName = serverEntry.Key;
                var server = serverEntry.Value;

                try
                {
                    // Replace parameter placeholders with user-specific values
                    var configuredServer = ReplaceParameterPlaceholders(server, userParameters);
                    
                    // Skip if required parameters are missing
                    if (!HasRequiredParameters(configuredServer))
                    {
                        _logger.LogInformation("Skipping MCP server {ServerName} for user {UserId} - missing required parameters", serverName, userId);
                        continue;
                    }

                    IMcpClient mcpClient;
                    if (server.Type.ToLower() == "stdio" || string.IsNullOrEmpty(server.Type))
                    {
                        mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
                        {
                            Name = serverName,
                            Command = configuredServer.Command ?? string.Empty,
                            Arguments = configuredServer.Args ?? new List<string>(),
                            EnvironmentVariables = configuredServer.Env?.ToDictionary(
                                kvp => kvp.Key, 
                                kvp => kvp.Value ?? string.Empty
                            ) ?? new Dictionary<string, string>()
                        }));
                    }
                    else if (server.Type.ToLower() == "sse")
                    {
                        var httpClient = _httpClientFactory.CreateClient("defaultHttpClient");
                        mcpClient = await McpClientFactory.CreateAsync(
                            new SseClientTransport(httpClient: httpClient, transportOptions: new SseClientTransportOptions()
                            {
                                Endpoint = new Uri(configuredServer.Url ?? string.Empty),
                                AdditionalHeaders = configuredServer.Headers ?? new Dictionary<string, string>()
                            }),
                            new McpClientOptions()
                            {
                                ClientInfo = new() { Name = serverName, Version = "1.0.0.0" }
                            });
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported server type: {server.Type}");
                    }

                    IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
                    var plugin = KernelPluginFactory.CreateFromFunctions(
                        serverName,
                        tools.Select(tool => tool.AsKernelFunction())
                    );
                    plugins.Add(plugin);
                    
                    _logger.LogInformation("Successfully created MCP plugin {ServerName} for user {UserId}", serverName, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error connecting to MCP server {serverName} for user {userId}: {ex.Message}");
                }
            }

            return plugins;
        }

        /// <summary>
        /// Sets a user's MCP parameter value (encrypted)
        /// </summary>
        public async Task SetUserMcpParameterAsync(string userId, string inputId, string value)
        {
            var encryptedValue = EncryptValue(value);
            
            var existingParameter = await _dbContext.UserMcpParameters
                .FirstOrDefaultAsync(p => p.UserId == userId && p.InputId == inputId);

            if (existingParameter != null)
            {
                existingParameter.EncryptedValue = encryptedValue;
                existingParameter.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.UserMcpParameters.Add(new UserMcpParameter
                {
                    UserId = userId,
                    InputId = inputId,
                    EncryptedValue = encryptedValue
                });
            }

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a user's decrypted MCP parameter value
        /// </summary>
        public async Task<string?> GetUserMcpParameterAsync(string userId, string inputId)
        {
            var parameter = await _dbContext.UserMcpParameters
                .FirstOrDefaultAsync(p => p.UserId == userId && p.InputId == inputId);

            return parameter != null ? DecryptValue(parameter.EncryptedValue) : null;
        }

        /// <summary>
        /// Gets all MCP parameters for a user
        /// </summary>
        public async Task<Dictionary<string, string>> GetUserMcpParametersAsync(string userId)
        {
            var parameters = await _dbContext.UserMcpParameters
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var result = new Dictionary<string, string>();
            foreach (var param in parameters)
            {
                result[param.InputId] = DecryptValue(param.EncryptedValue);
            }

            return result;
        }

        /// <summary>
        /// Gets the list of required MCP inputs from configuration
        /// </summary>
        public List<McpInput> GetRequiredMcpInputs()
        {
            return _appSettings.Mcp?.Inputs ?? new List<McpInput>();
        }

        /// <summary>
        /// Deletes a user's MCP parameter
        /// </summary>
        public async Task DeleteUserMcpParameterAsync(string userId, string inputId)
        {
            var parameter = await _dbContext.UserMcpParameters
                .FirstOrDefaultAsync(p => p.UserId == userId && p.InputId == inputId);

            if (parameter != null)
            {
                _dbContext.UserMcpParameters.Remove(parameter);
                await _dbContext.SaveChangesAsync();
            }
        }

        private McpServer ReplaceParameterPlaceholders(McpServer originalServer, Dictionary<string, string> userParameters)
        {
            var configuredServer = new McpServer
            {
                Type = originalServer.Type,
                Command = ReplaceTokens(originalServer.Command, userParameters),
                Args = originalServer.Args?.Select(arg => ReplaceTokens(arg, userParameters) ?? string.Empty).ToList(),
                Env = originalServer.Env?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ReplaceTokens(kvp.Value, userParameters) ?? string.Empty
                ),
                Url = ReplaceTokens(originalServer.Url, userParameters),
                Headers = originalServer.Headers?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ReplaceTokens(kvp.Value, userParameters) ?? string.Empty
                )
            };

            return configuredServer;
        }

        private string? ReplaceTokens(string? input, Dictionary<string, string> userParameters)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Replace ${input:parameter_name} tokens with user parameter values
            var pattern = @"\$\{input:([^}]+)\}";
            return Regex.Replace(input, pattern, match =>
            {
                var parameterName = match.Groups[1].Value;
                return userParameters.TryGetValue(parameterName, out var value) ? value : match.Value;
            });
        }

        private bool HasRequiredParameters(McpServer server)
        {
            // Check if any placeholder tokens remain (indicating missing parameters)
            var pattern = @"\$\{input:[^}]+\}";
            
            var allValues = new List<string?>
            {
                server.Command,
                server.Url
            };
            
            if (server.Args != null)
                allValues.AddRange(server.Args);
                
            if (server.Env != null)
                allValues.AddRange(server.Env.Values);
                
            if (server.Headers != null)
                allValues.AddRange(server.Headers.Values);

            return !allValues.Any(value => !string.IsNullOrEmpty(value) && Regex.IsMatch(value, pattern));
        }

        private string EncryptValue(string value)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(EncryptionKey);
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plainTextBytes = Encoding.UTF8.GetBytes(value);
                var encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);

                // Prepend IV to encrypted data
                var result = new byte[aes.IV.Length + encryptedBytes.Length];
                aes.IV.CopyTo(result, 0);
                encryptedBytes.CopyTo(result, aes.IV.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting MCP parameter value");
                throw;
            }
        }

        private string DecryptValue(string encryptedValue)
        {
            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedValue);
                
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(EncryptionKey);

                // Extract IV from the beginning of encrypted data
                var iv = new byte[aes.IV.Length];
                var encrypted = new byte[encryptedBytes.Length - iv.Length];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                Array.Copy(encryptedBytes, iv.Length, encrypted, 0, encrypted.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting MCP parameter value");
                throw;
            }
        }
    }
}