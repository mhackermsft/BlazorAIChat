#pragma warning disable SKEXP0010, SKEXP0001, SKEXP0020, KMEXP00
using Microsoft.SemanticKernel;
using BlazorAIChat.Models;

namespace BlazorAIChat.Services
{
    public class McpPluginProvider
    {
        private readonly UserMcpService _userMcpService;
        private readonly ILogger<McpPluginProvider> _logger;

        public McpPluginProvider(UserMcpService userMcpService, ILogger<McpPluginProvider> logger)
        {
            _userMcpService = userMcpService;
            _logger = logger;
        }

        /// <summary>
        /// Gets MCP plugins for a specific user
        /// </summary>
        public async Task<List<KernelPlugin>> GetUserPluginsAsync(string userId)
        {
            try
            {
                return await _userMcpService.GetUserMcpPluginsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MCP plugins for user {UserId}", userId);
                return new List<KernelPlugin>();
            }
        }
    }
}
