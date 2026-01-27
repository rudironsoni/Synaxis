#!/bin/bash

# Configuration
BASE_URL="http://localhost:5042/v1/chat/completions"
TIMEOUT=30

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# List of models to test (Canonical IDs)
MODELS=(
    # Groq
    "groq/llama-3.3-70b"
    "groq/llama-3.1-70b"
    "groq/llama-3.1-8b"
    "groq/mixtral-8x7b"
    "groq/gemma2-9b"
    "groq/deepseek-r1-distill"

    # Cohere
    "cohere/command-r-plus"
    "cohere/command-r"
    "cohere/command-light"
    "cohere/aya-expanse-32b"
    "cohere/aya-expanse-8b"

    # Gemini
    "gemini/flash-2.0-exp"
    "gemini/pro-1.5"
    "gemini/flash-1.5"
    "gemini/flash-1.5-8b"
    "gemini/pro-1.0"

    # Cloudflare
    "cloudflare/llama-3.1-8b"
    "cloudflare/llama-3.2-3b"
    "cloudflare/mistral-7b"
    "cloudflare/phi-2"
    "cloudflare/qwen-1.5-7b"

    # NVIDIA
    "nvidia/llama-3.3-70b"
    "nvidia/nemotron-70b"
    "nvidia/mixtral-8x7b"
    "nvidia/deepseek-v3"
    "nvidia/nemotron-nano"

    # HuggingFace
    "hf/llama-3.3-70b"
    "hf/mistral-7b-v0.3"
    "hf/phi-3-mini"
    "hf/gemma-2-9b"

    # OpenRouter
    "openrouter/gemini-2.0-free"
    "openrouter/llama-3.3-free"
    "openrouter/mistral-7b-free"
    "openrouter/phi-3-free"
    "openrouter/zephyr-free"
    "openrouter/openchat-free"

    # Pollinations
    "pollinations/openai"
    "pollinations/mistral"
    "pollinations/llama"
    "pollinations/search"
)

echo "Starting Model Verification Test..."
echo "Target: $BASE_URL"
echo "----------------------------------------"

PASSED=0
FAILED=0

for model in "${MODELS[@]}"; do
    echo -n "Testing $model... "
    
    response=$(curl -s -X POST "$BASE_URL" \
        -H "Content-Type: application/json" \
        -d "{
            \"model\": \"$model\",
            \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}],
            \"max_tokens\": 10
        }" \
        --max-time $TIMEOUT)

    # Check if response contains "choices" (success) or "error"
    if echo "$response" | grep -q "\"choices\""; then
        echo -e "${GREEN}PASS${NC}"
        ((PASSED++))
    else
        echo -e "${RED}FAIL${NC}"
        echo "Response: $response"
        ((FAILED++))
    fi
    
    # Small delay to respect rate limits
    sleep 1
done

echo "----------------------------------------"
echo "Test Complete."
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"

if [ $FAILED -eq 0 ]; then
    exit 0
else
    exit 1
fi
