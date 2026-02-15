# Multi-Modal Example (Python)

This example demonstrates how to use Synaxis for multi-modal AI tasks including text, images, and audio using Python.

## Prerequisites

- **Python** 3.8+ installed
- **Synaxis Gateway** running (or use the hosted API)
- **API Key** for at least one AI provider

## Setup

### Install Dependencies

```bash
pip install requests pillow python-dotenv
```

### Environment Variables

Create a `.env` file:

```bash
SYNAXIS_API_URL=http://localhost:8080
SYNAXIS_API_KEY=sk-your-openai-key
```

### Python Script Setup

```python
import os
from dotenv import load_dotenv

load_dotenv()

API_URL = os.getenv('SYNAXIS_API_URL', 'http://localhost:8080')
API_KEY = os.getenv('SYNAXIS_API_KEY', 'sk-your-openai-key')
```

## Text Generation

### Basic Text Completion

```python
import requests

def generate_text(prompt, model='gpt-4'):
    response = requests.post(
        f'{API_URL}/v1/chat/completions',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': model,
            'messages': [
                {'role': 'user', 'content': prompt}
            ]
        }
    )

    if response.status_code == 200:
        data = response.json()
        return data['choices'][0]['message']['content']
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
text = generate_text('Write a haiku about programming.')
print(text)
```

### Streaming Text Generation

```python
import requests
import json

def stream_text(prompt, model='gpt-4'):
    response = requests.post(
        f'{API_URL}/v1/chat/completions',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': model,
            'messages': [
                {'role': 'user', 'content': prompt}
            ],
            'stream': True
        },
        stream=True
    )

    for line in response.iter_lines():
        if line:
            line = line.decode('utf-8')
            if line.startswith('data: '):
                data = line[6:]
                if data == '[DONE]':
                    break
                try:
                    parsed = json.loads(data)
                    content = parsed['choices'][0]['delta'].get('content', '')
                    if content:
                        print(content, end='', flush=True)
                except json.JSONDecodeError:
                    pass
    print()  # New line at the end

# Usage
stream_text('Tell me a story about a space explorer.')
```

## Image Generation

### Generate Image from Text

```python
import requests
from PIL import Image
from io import BytesIO

def generate_image(prompt, size='1024x1024', model='dall-e-3'):
    response = requests.post(
        f'{API_URL}/v1/images/generations',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': model,
            'prompt': prompt,
            'n': 1,
            'size': size
        }
    )

    if response.status_code == 200:
        data = response.json()
        image_url = data['data'][0]['url']

        # Download the image
        img_response = requests.get(image_url)
        img = Image.open(BytesIO(img_response.content))
        return img
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
image = generate_image('A futuristic city with flying cars at sunset')
image.save('generated_image.png')
print('Image saved as generated_image.png')
```

### Generate Multiple Images

```python
def generate_multiple_images(prompt, n=4, size='512x512'):
    response = requests.post(
        f'{API_URL}/v1/images/generations',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': 'dall-e-2',
            'prompt': prompt,
            'n': n,
            'size': size
        }
    )

    if response.status_code == 200:
        data = response.json()
        images = []

        for i, item in enumerate(data['data']):
            img_response = requests.get(item['url'])
            img = Image.open(BytesIO(img_response.content))
            images.append(img)
            img.save(f'generated_image_{i+1}.png')

        return images
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
images = generate_multiple_images('Abstract art with vibrant colors', n=4)
print(f'Generated {len(images)} images')
```

## Image Analysis

### Analyze Image with Vision

```python
import base64

def encode_image(image_path):
    with open(image_path, 'rb') as image_file:
        return base64.b64encode(image_file.read()).decode('utf-8')

def analyze_image(image_path, prompt='Describe this image in detail.'):
    base64_image = encode_image(image_path)

    response = requests.post(
        f'{API_URL}/v1/chat/completions',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': 'gpt-4-vision-preview',
            'messages': [
                {
                    'role': 'user',
                    'content': [
                        {'type': 'text', 'text': prompt},
                        {
                            'type': 'image_url',
                            'image_url': {
                                'url': f'data:image/jpeg;base64,{base64_image}'
                            }
                        }
                    ]
                }
            ],
            'max_tokens': 300
        }
    )

    if response.status_code == 200:
        data = response.json()
        return data['choices'][0]['message']['content']
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
description = analyze_image('path/to/your/image.jpg', 'What do you see in this image?')
print(description)
```

## Audio Processing

### Text-to-Speech

```python
import requests
from pydub import AudioSegment
from pydub.playback import play

def text_to_speech(text, voice='alloy', model='tts-1'):
    response = requests.post(
        f'{API_URL}/v1/audio/speech',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': model,
            'input': text,
            'voice': voice
        }
    )

    if response.status_code == 200:
        # Save audio file
        with open('speech.mp3', 'wb') as f:
            f.write(response.content)

        # Play audio (requires pydub and simpleaudio)
        audio = AudioSegment.from_mp3('speech.mp3')
        play(audio)

        return 'speech.mp3'
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
text_to_speech('Hello! This is a text-to-speech example.')
```

### Speech-to-Text (Transcription)

```python
def transcribe_audio(audio_path, model='whisper-1'):
    with open(audio_path, 'rb') as audio_file:
        response = requests.post(
            f'{API_URL}/v1/audio/transcriptions',
            headers={
                'Authorization': f'Bearer {API_KEY}'
            },
            files={
                'file': audio_file,
                'model': (None, model)
            }
        )

    if response.status_code == 200:
        data = response.json()
        return data['text']
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
transcription = transcribe_audio('path/to/audio.mp3')
print(f'Transcription: {transcription}')
```

### Speech-to-Text with Timestamps

```python
def transcribe_with_timestamps(audio_path, model='whisper-1'):
    with open(audio_path, 'rb') as audio_file:
        response = requests.post(
            f'{API_URL}/v1/audio/transcriptions',
            headers={
                'Authorization': f'Bearer {API_KEY}'
            },
            files={
                'file': audio_file,
                'model': (None, model),
                'response_format': (None, 'verbose_json')
            }
        )

    if response.status_code == 200:
        data = response.json()
        return {
            'text': data['text'],
            'words': data.get('words', [])
        }
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
result = transcribe_with_timestamps('path/to/audio.mp3')
print(f'Text: {result["text"]}')
for word in result['words']:
    print(f"  {word['word']}: {word['start']:.2f}s - {word['end']:.2f}s")
```

## Multi-Modal Pipeline

### Text → Image → Description

```python
def text_to_image_to_description(text_prompt):
    # Step 1: Generate image from text
    print('Generating image...')
    image = generate_image(text_prompt)
    image.save('intermediate_image.png')

    # Step 2: Analyze the generated image
    print('Analyzing image...')
    description = analyze_image(
        'intermediate_image.png',
        'Describe this image in detail, including colors, composition, and mood.'
    )

    return {
        'original_prompt': text_prompt,
        'image_path': 'intermediate_image.png',
        'description': description
    }

# Usage
result = text_to_image_to_description('A serene mountain landscape at dawn')
print(f'\nOriginal Prompt: {result["original_prompt"]}')
print(f'\nDescription:\n{result["description"]}')
```

### Audio → Text → Image

```python
def audio_to_text_to_image(audio_path):
    # Step 1: Transcribe audio
    print('Transcribing audio...')
    transcription = transcribe_audio(audio_path)
    print(f'Transcription: {transcription}')

    # Step 2: Generate image from transcription
    print('Generating image from transcription...')
    image = generate_image(transcription[:400])  # Limit prompt length
    image.save('audio_to_image.png')

    return {
        'audio_path': audio_path,
        'transcription': transcription,
        'image_path': 'audio_to_image.png'
    }

# Usage
result = audio_to_text_to_image('path/to/audio.mp3')
print(f'\nTranscription: {result["transcription"]}')
print(f'Image saved as: {result["image_path"]}')
```

## Embeddings

### Generate Text Embeddings

```python
def generate_embeddings(text, model='text-embedding-ada-002'):
    response = requests.post(
        f'{API_URL}/v1/embeddings',
        headers={
            'Authorization': f'Bearer {API_KEY}',
            'Content-Type': 'application/json'
        },
        json={
            'model': model,
            'input': text
        }
    )

    if response.status_code == 200:
        data = response.json()
        return data['data'][0]['embedding']
    else:
        raise Exception(f'Error: {response.status_code} - {response.text}')

# Usage
embedding = generate_embeddings('Hello, world!')
print(f'Embedding dimension: {len(embedding)}')
print(f'First 10 values: {embedding[:10]}')
```

### Semantic Search with Embeddings

```python
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity

def semantic_search(query, documents):
    # Generate embedding for query
    query_embedding = np.array(generate_embeddings(query)).reshape(1, -1)

    # Generate embeddings for documents
    doc_embeddings = []
    for doc in documents:
        embedding = generate_embeddings(doc)
        doc_embeddings.append(embedding)

    doc_embeddings = np.array(doc_embeddings)

    # Calculate similarities
    similarities = cosine_similarity(query_embedding, doc_embeddings)[0]

    # Sort by similarity
    ranked_docs = sorted(zip(documents, similarities), key=lambda x: x[1], reverse=True)

    return ranked_docs

# Usage
documents = [
    'The quick brown fox jumps over the lazy dog.',
    'Python is a popular programming language.',
    'Machine learning is a subset of artificial intelligence.',
    'The weather is beautiful today.'
]

query = 'programming languages'
results = semantic_search(query, documents)

print(f'Query: {query}\n')
for doc, similarity in results:
    print(f'Similarity: {similarity:.3f} - {doc}')
```

## Error Handling

### Robust Multi-Modal Function

```python
import time
from typing import Optional

def safe_multi_modal_request(
    endpoint: str,
    payload: dict,
    max_retries: int = 3,
    retry_delay: int = 2
) -> Optional[dict]:
    """Make a safe request with retry logic."""
    attempt = 0

    while attempt < max_retries:
        try:
            response = requests.post(
                f'{API_URL}{endpoint}',
                headers={
                    'Authorization': f'Bearer {API_KEY}',
                    'Content-Type': 'application/json'
                },
                json=payload,
                timeout=30
            )

            if response.status_code == 429:
                # Rate limited
                retry_after = int(response.headers.get('Retry-After', retry_delay))
                print(f'Rate limited. Waiting {retry_after} seconds...')
                time.sleep(retry_after)
                attempt += 1
                continue

            if response.status_code == 200:
                return response.json()
            else:
                error_data = response.json()
                print(f'Error: {error_data.get("error", {}).get("message", "Unknown error")}')
                return None

        except requests.exceptions.Timeout:
            print(f'Request timed out. Attempt {attempt + 1}/{max_retries}')
            attempt += 1
            time.sleep(retry_delay)
        except requests.exceptions.RequestException as e:
            print(f'Request failed: {e}')
            attempt += 1
            time.sleep(retry_delay)

    print(f'Failed after {max_retries} attempts')
    return None

# Usage
result = safe_multi_modal_request(
    '/v1/chat/completions',
    {
        'model': 'gpt-4',
        'messages': [{'role': 'user', 'content': 'Hello!'}]
    }
)

if result:
    print(result['choices'][0]['message']['content'])
```

## Tips and Best Practices

1. **Handle large files** by chunking uploads
2. **Use streaming** for long text generation
3. **Cache embeddings** for repeated queries
4. **Validate inputs** before sending to API
5. **Implement retry logic** for rate limits
6. **Monitor usage** with token counts
7. **Use appropriate models** for each task

## Next Steps

- [Simple Chat Example](./simple-chat-curl.md) - Basic chat completion
- [Streaming Example](./streaming-javascript.md) - Streaming responses
- [Batch Processing Example](./batch-processing-csharp.md) - Process multiple requests

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord**: [Join our Discord](https://discord.gg/synaxis)
