using Microsoft.KernelMemory.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace BlazorAIChat.Utils
{
    public static class AIUtils
    {
#pragma warning disable  KMEXP00

        public static ChatHistory CleanUpHistory(ChatHistory history, ITextTokenizer tokenizer, int MaxTokens)
        {

            string fullContext = CreateStringFromHistory(history);

            while (tokenizer.CountTokens(fullContext) >= MaxTokens)
            {
                foreach (var m in history)
                {
                    if (m.Role!= AuthorRole.System)
                    {
                        history.Remove(m);
                        break;
                    }
                }

                fullContext =  CreateStringFromHistory(history);
            }

            return history;
        }

        public static string CreateStringFromHistory(ChatHistory history)
        {
            StringBuilder fullContext = new StringBuilder();
            foreach (var m in history)
                fullContext.Append(m.ToString());
            return fullContext.ToString();
        }
    }
}
