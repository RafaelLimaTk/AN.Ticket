using AN.Ticket.Application.Helpers.OpenAI;
using AN.Ticket.Application.Interfaces;
using OpenAI_API;
using System.Text.Json;

namespace AN.Ticket.Application.Services;
public class ChatGptService : IChatGptService
{
    private const string CHAT_GPT_MODEL = "gpt-3.5-turbo";

    private readonly IOpenAIAPI _openAIAPI;

    public ChatGptService(
        IOpenAIAPI openAIAPI
    )
    {
        _openAIAPI = openAIAPI;
    }

    public async Task<List<MessageResponse>> GenerateResponseAsync(string message)
    {
        var conversation = _openAIAPI.Chat.CreateConversation();
        conversation.AppendSystemMessage(@"Você é um assistente de suporte de helpdesk:
                                Reformule o conteúdo da mensagem de e-mail recebida em uma estrutura organizada, facilitando a extração das informações como remetente, data e conteúdo da mensagem. Cada mensagem deve ser formatada claramente para posterior processamento e armazenamento em um banco de dados.

                                Instruções:
                                1. Para cada mensagem presente no histórico de e-mails, extraia e organize as seguintes informações:
                                   - Remetente: O nome e o e-mail da pessoa que enviou a mensagem.
                                   - Data: A data e a hora em que a mensagem foi enviada.
                                   - Mensagem: O conteúdo da mensagem enviado por esse remetente.

                                2. Reformule o e-mail completo, separando claramente cada uma das mensagens e seus remetentes. Use o formato JSON fornecido abaixo para estruturar a saída.

                                Formato da Reescrita:

                                {
                                  ""mensagens"": [
                                    {
                                      ""remetente"": ""Nome e-mail do remetente"",
                                      ""data"": ""Data e hora do envio"",
                                      ""mensagem"": ""Conteúdo da mensagem""
                                    },
                                    {
                                      ""remetente"": ""Nome e-mail do remetente"",
                                      ""data"": ""Data e hora do envio"",
                                      ""mensagem"": ""Conteúdo da mensagem""
                                    }
                                    // Adicione outras mensagens aqui
                                  ]
                                }

                                Exemplo de Entrada:

                                Em sáb., 14 de set. de 2024 às 23:25, DevDive Rafael <devdivetk@gmail.com>
                                escreveu:

                                > Você  tá chamando duas vezes ?
                                >
                                > Em sáb., 14 de set. de 2024 às 23:13, Spider Tech NT <conectwolrdspidertech@gmail.com> escreveu:
                                >
                                >> Teste 2
                                >>
                                >> Em dom., 1 de set. de 2024 às 21:54, DevDive Rafael <devdivetk@gmail.com>
                                >> escreveu:
                                >>
                                >>> olá
                                >>>
                                >>> On Sun, Sep 1, 2024 at 9:50 PM DevDive Rafael <devdivetk@gmail.com>
                                >>> wrote:
                                >>>
                                >>>> teste
                                >>>>
                                >>>> On Sun, Sep 1, 2024 at 9:41 PM DevDive Rafael <devdivetk@gmail.com>
                                >>>> wrote:
                                >>>>
                                >>>>> Olá

                                Exemplo de Saída Esperada:

                                {
                                  ""mensagens"": [
                                    {
                                      ""remetente"": ""DevDive Rafael <devdivetk@gmail.com>"",
                                      ""data"": ""14 de set. de 2024 às 23:25"",
                                      ""mensagem"": ""Você tá chamando duas vezes?""
                                    },
                                    {
                                      ""remetente"": ""Spider Tech NT <conectwolrdspidertech@gmail.com>"",
                                      ""data"": ""14 de set. de 2024 às 23:13"",
                                      ""mensagem"": ""Teste 2""
                                    },
                                    {
                                      ""remetente"": ""DevDive Rafael <devdivetk@gmail.com>"",
                                      ""data"": ""1 de set. de 2024 às 21:54"",
                                      ""mensagem"": ""olá""
                                    },
                                    {
                                      ""remetente"": ""DevDive Rafael <devdivetk@gmail.com>"",
                                      ""data"": ""1 de set. de 2024 às 21:50"",
                                      ""mensagem"": ""teste""
                                    },
                                    {
                                      ""remetente"": ""DevDive Rafael <devdivetk@gmail.com>"",
                                      ""data"": ""1 de set. de 2024 às 21:41"",
                                      ""mensagem"": ""Olá""
                                    }
                                  ]
                                }
                                Não responda mas nada além do que foi solicitado.");

        conversation.AppendUserInput(message);
        var response = await conversation.GetResponseFromChatbotAsync();

        var responseObject = JsonSerializer.Deserialize<ResponseObject>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return responseObject?.Mensagens ?? new List<MessageResponse>();
    }
}

public class ResponseObject
{
    public List<MessageResponse> Mensagens { get; set; }
}
