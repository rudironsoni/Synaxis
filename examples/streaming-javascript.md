# Streaming Example (JavaScript)

This example demonstrates how to use streaming responses with Synaxis using JavaScript and the Fetch API.

## Prerequisites

- **Node.js** 18+ installed
- **Synaxis Gateway** running (or use the hosted API)
- **API Key** for at least one AI provider

## Setup

### Environment Variables

Create a `.env` file:

```bash
SYNAXIS_API_URL=http://localhost:8080
SYNAXIS_API_KEY=sk-your-openai-key
```

Or set directly in code:

```javascript
const API_URL = 'http://localhost:8080';
const API_KEY = 'sk-your-openai-key';
```

## Basic Streaming Example

### Code

```javascript
const API_URL = 'http://localhost:8080';
const API_KEY = 'sk-your-openai-key';

async function streamChatCompletion() {
  const response = await fetch(`${API_URL}/v1/chat/completions`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${API_KEY}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      model: 'gpt-4',
      messages: [
        { role: 'user', content: 'Tell me a short story about a robot.' }
      ],
      stream: true
    })
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      const chunk = decoder.decode(value);
      const lines = chunk.split('\n');

      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const data = line.slice(6);
          if (data === '[DONE]') {
            console.log('\n[Stream completed]');
            return;
          }

          try {
            const parsed = JSON.parse(data);
            const content = parsed.choices[0]?.delta?.content;
            if (content) {
              process.stdout.write(content);
            }
          } catch (e) {
            // Skip invalid JSON
          }
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}

streamChatCompletion().catch(console.error);
```

### Run

```bash
node streaming-example.js
```

## Advanced Streaming with UI Updates

### Code

```javascript
const API_URL = 'http://localhost:8080';
const API_KEY = 'sk-your-openai-key';

class StreamingChat {
  constructor() {
    this.messages = [];
    this.isStreaming = false;
  }

  async sendMessage(userMessage) {
    if (this.isStreaming) {
      console.log('Already streaming...');
      return;
    }

    this.messages.push({ role: 'user', content: userMessage });
    this.isStreaming = true;

    console.log(`\nUser: ${userMessage}`);
    process.stdout.write('Assistant: ');

    const response = await fetch(`${API_URL}/v1/chat/completions`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${API_KEY}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        model: 'gpt-4',
        messages: this.messages,
        stream: true
      })
    });

    if (!response.ok) {
      this.isStreaming = false;
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let assistantMessage = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value);
        const lines = chunk.split('\n');

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const data = line.slice(6);
            if (data === '[DONE]') {
              break;
            }

            try {
              const parsed = JSON.parse(data);
              const content = parsed.choices[0]?.delta?.content;
              if (content) {
                process.stdout.write(content);
                assistantMessage += content;
              }
            } catch (e) {
              // Skip invalid JSON
            }
          }
        }
      }

      this.messages.push({ role: 'assistant', content: assistantMessage });
    } finally {
      reader.releaseLock();
      this.isStreaming = false;
      console.log('\n');
    }
  }
}

// Usage
const chat = new StreamingChat();

async function main() {
  await chat.sendMessage('Hello! How are you?');
  await chat.sendMessage('Tell me a joke.');
  await chat.sendMessage('What can you do?');
}

main().catch(console.error);
```

## Streaming with Error Handling

### Code

```javascript
const API_URL = 'http://localhost:8080';
const API_KEY = 'sk-your-openai-key';

async function streamWithRetry(prompt, maxRetries = 3) {
  let attempt = 0;

  while (attempt < maxRetries) {
    try {
      const response = await fetch(`${API_URL}/v1/chat/completions`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${API_KEY}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          model: 'gpt-4',
          messages: [{ role: 'user', content: prompt }],
          stream: true
        })
      });

      if (response.status === 429) {
        // Rate limited - wait and retry
        const retryAfter = parseInt(response.headers.get('Retry-After') || '5');
        console.log(`Rate limited. Waiting ${retryAfter} seconds...`);
        await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
        attempt++;
        continue;
      }

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error?.message || 'Unknown error');
      }

      return response;

    } catch (error) {
      attempt++;
      if (attempt >= maxRetries) {
        throw error;
      }
      console.log(`Attempt ${attempt} failed. Retrying...`);
      await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
    }
  }
}

async function streamChatWithRetry() {
  try {
    const response = await streamWithRetry('Tell me a story.');
    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      const chunk = decoder.decode(value);
      const lines = chunk.split('\n');

      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const data = line.slice(6);
          if (data === '[DONE]') break;

          try {
            const parsed = JSON.parse(data);
            const content = parsed.choices[0]?.delta?.content;
            if (content) {
              process.stdout.write(content);
            }
          } catch (e) {
            // Skip invalid JSON
          }
        }
      }
    }
  } catch (error) {
    console.error('Error:', error.message);
  }
}

streamChatWithRetry();
```

## Streaming with Progress Indicator

### Code

```javascript
const API_URL = 'http://localhost:8080';
const API_KEY = 'sk-your-openai-key';

async function streamWithProgress(prompt) {
  const response = await fetch(`${API_URL}/v1/chat/completions`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${API_KEY}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      model: 'gpt-4',
      messages: [{ role: 'user', content: prompt }],
      stream: true
    })
  });

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let tokenCount = 0;
  let startTime = Date.now();

  console.log('Streaming response...\n');

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    const chunk = decoder.decode(value);
    const lines = chunk.split('\n');

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = line.slice(6);
        if (data === '[DONE]') break;

        try {
          const parsed = JSON.parse(data);
          const content = parsed.choices[0]?.delta?.content;
          if (content) {
            process.stdout.write(content);
            tokenCount++;

            // Update progress every 10 tokens
            if (tokenCount % 10 === 0) {
              const elapsed = (Date.now() - startTime) / 1000;
              const tokensPerSecond = (tokenCount / elapsed).toFixed(1);
              process.stderr.write(`\n[${tokenCount} tokens, ${tokensPerSecond} tokens/s] `);
            }
          }
        } catch (e) {
          // Skip invalid JSON
        }
      }
    }

    const elapsed = (Date.now() - startTime) / 1000;
    const tokensPerSecond = (tokenCount / elapsed).toFixed(1);
    console.log(`\n\nCompleted: ${tokenCount} tokens in ${elapsed.toFixed(1)}s (${tokensPerSecond} tokens/s)`);
  }
}

streamWithProgress('Write a detailed explanation of how neural networks work.');
```

## Browser Example

### HTML

```html
<!DOCTYPE html>
<html>
<head>
  <title>Synaxis Streaming Example</title>
  <style>
    body {
      font-family: Arial, sans-serif;
      max-width: 800px;
      margin: 50px auto;
      padding: 20px;
    }
    #chat-container {
      border: 1px solid #ccc;
      padding: 20px;
      margin-bottom: 20px;
      min-height: 200px;
      max-height: 400px;
      overflow-y: auto;
    }
    #user-input {
      width: 70%;
      padding: 10px;
    }
    #send-button {
      padding: 10px 20px;
      background: #007bff;
      color: white;
      border: none;
      cursor: pointer;
    }
    .message {
      margin: 10px 0;
      padding: 10px;
      border-radius: 5px;
    }
    .user-message {
      background: #e3f2fd;
      text-align: right;
    }
    .assistant-message {
      background: #f5f5f5;
    }
  </style>
</head>
<body>
  <h1>Synaxis Streaming Chat</h1>
  <div id="chat-container"></div>
  <input type="text" id="user-input" placeholder="Type your message...">
  <button id="send-button">Send</button>

  <script>
    const API_URL = 'http://localhost:8080';
    const API_KEY = 'sk-your-openai-key';

    const chatContainer = document.getElementById('chat-container');
    const userInput = document.getElementById('user-input');
    const sendButton = document.getElementById('send-button');

    function appendMessage(role, content) {
      const messageDiv = document.createElement('div');
      messageDiv.className = `message ${role}-message`;
      messageDiv.textContent = content;
      chatContainer.appendChild(messageDiv);
      chatContainer.scrollTop = chatContainer.scrollHeight;
      return messageDiv;
    }

    async function sendMessage() {
      const message = userInput.value.trim();
      if (!message) return;

      userInput.value = '';
      appendMessage('user', message);

      const assistantMessage = appendMessage('assistant', '');
      assistantMessage.textContent = 'Thinking...';

      try {
        const response = await fetch(`${API_URL}/v1/chat/completions`, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${API_KEY}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            model: 'gpt-4',
            messages: [{ role: 'user', content: message }],
            stream: true
          })
        });

        assistantMessage.textContent = '';

        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          const chunk = decoder.decode(value);
          const lines = chunk.split('\n');

          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const data = line.slice(6);
              if (data === '[DONE]') break;

              try {
                const parsed = JSON.parse(data);
                const content = parsed.choices[0]?.delta?.content;
                if (content) {
                  assistantMessage.textContent += content;
                  chatContainer.scrollTop = chatContainer.scrollHeight;
                }
              } catch (e) {
                // Skip invalid JSON
              }
            }
          }
        }
      } catch (error) {
        assistantMessage.textContent = `Error: ${error.message}`;
      }
    }

    sendButton.addEventListener('click', sendMessage);
    userInput.addEventListener('keypress', (e) => {
      if (e.key === 'Enter') sendMessage();
    });
  </script>
</body>
</html>
```

## Tips and Best Practices

1. **Handle errors gracefully** with retry logic
2. **Use backpressure** for slow consumers
3. **Display progress** to improve user experience
4. **Buffer chunks** for better performance
5. **Clean up resources** with `finally` blocks
6. **Validate responses** before parsing JSON
7. **Use AbortController** for cancellation

## Next Steps

- [Simple Chat Example](./simple-chat-curl.md) - Basic chat completion
- [Multi-Modal Example](./multimodal-python.md) - Work with images and audio
- [Batch Processing Example](./batch-processing-csharp.md) - Process multiple requests

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord**: [Join our Discord](https://discord.gg/synaxis)
