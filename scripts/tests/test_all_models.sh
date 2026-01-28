#!/bin/bash

# Configuration
# Default to port 8080 (Docker Compose default)
BASE_URL="${BASE_URL:-http://localhost:8080/openai/v1/chat/completions}"
TIMEOUT=30

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# List of models to test (Sync with appsettings.json)
MODELS=(
    # Groq
    "groq/llama-3.3-70b"
    "groq/llama-3.1-8b"
    

    # Cohere
    
    "cohere/c4ai-aya-expanse-32b"
    "cohere/c4ai-aya-expanse-8b"

    # Gemini
    "gemini/flash-2.0"
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
    "hf/phi-3.5-mini"
    "hf/smollm2-1.7b"
    "hf/qwen-2.5-7b"
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
)

echo "Starting Enhanced Model Verification Test..."
echo "Target: $BASE_URL"
echo "--------------------------------------------------------"

PASSED=0
FAILED=0

for model in "${MODELS[@]}"; do
    printf "Testing %-35s " "$model..."
    
    # Capture HTTP Code and Body separately
    # -w %{http_code} prints status at the end
    response=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL" \
        -H "Content-Type: application/json" \
        -d "{
            \"model\": \"$model\",
            \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}],
            \"max_tokens\": 10
        }" \
        --max-time $TIMEOUT)

    # Extract Body and Code
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    if [ "$http_code" -eq 200 ]; then
        echo -e "[${GREEN}PASS${NC}] (200 OK)"
        ((PASSED++))
    else
        echo -e "[${RED}FAIL${NC}] ($http_code)"
        ((FAILED++))
        
        # Error Analysis
        case $http_code in
            401)
                echo -e "  -> ${RED}❌ Invalid API Key / Auth Failure${NC}"
                ;;
            429)
                echo -e "  -> ${YELLOW}⚠️ Quota Exceeded / Rate Limit${NC}"
                ;;
            400|404)
                echo -e "  -> ${BLUE}🚫 Model Config Error / Decommissioned${NC}"
                ;;
            502)
                echo -e "  -> ${YELLOW}☁️ Upstream Provider Failure${NC}"
                ;;
            *)
                echo -e "  -> ${RED}💥 Unknown Error${NC}"
                ;;
        esac

        # Extract 'message' using jq if available, otherwise raw body
        if command -v jq &> /dev/null; then
            # Try to parse our structured error format
            error_msg=$(echo "$body" | jq -r '.error.message // empty')
            if [ -n "$error_msg" ]; then
                echo -e "  -> Details: \"$error_msg\""
            else
                # Fallback for non-JSON or weird responses
                echo "  -> Body: $body"
            fi
        else
            echo "  -> Body: $body"
        fi
    fi
    
    # Small delay to respect rate limits
    sleep 0.5
done

echo "--------------------------------------------------------"
echo "Test Complete."
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"

if [ $FAILED -eq 0 ]; then
    exit 0
else
    exit 1
fi
