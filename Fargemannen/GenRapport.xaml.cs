using System;
using System.Windows;
using OpenAI_API;
using OpenAI_API.Completions;

namespace Fargemannen
{
    public partial class GenRapport : Window
    {
        private OpenAIAPI openAiApi;

        public GenRapport()
        {
            InitializeComponent();

            var openAiApiKey = "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"; 
            APIAuthentication apiAuthentication = new APIAuthentication(openAiApiKey);
            openAiApi = new OpenAIAPI(apiAuthentication);
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = InputTextBox.Text;
            string model = "gpt-3.5-turbo-instruct";
            int maxTokens = 50;

            var completionRequest = new CompletionRequest
            {
                Prompt = prompt,
                Model = model,
                MaxTokens = maxTokens
            };

            try
            {
                var completionResult = await openAiApi.Completions.CreateCompletionAsync(completionRequest);
                var generatedText = completionResult.Completions[0].Text;
                OutputTextBlock.Text = generatedText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}