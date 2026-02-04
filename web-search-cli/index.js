#!/usr/bin/env node

const { program } = require('commander');
const { BingChat } = require('bing-chat');
const Anthropic = require('@anthropic-ai/sdk');
require('dotenv').config();

// Configure CLI
program
  .name('web-search')
  .description('CLI tool to search the web using Sonar API and get AI-generated answers')
  .version('1.0.0')
  .argument('<question>', 'The question you want to ask')
  .option('-m, --model <model>', 'Claude model to use', 'claude-3-5-sonnet-20241022')
  .option('-c, --conversation-style <style>', 'Bing conversation style', 'balanced')
  .parse();

const options = program.opts();
const question = program.args[0];

// Validate environment variables
if (!process.env.BING_COOKIE) {
  console.error('Error: BING_COOKIE environment variable is required');
  console.error('Please set it in your .env file or environment');
  process.exit(1);
}

if (!process.env.ANTHROPIC_API_KEY) {
  console.error('Error: ANTHROPIC_API_KEY environment variable is required');
  console.error('Please set it in your .env file or environment');
  process.exit(1);
}

// Initialize clients
const bingChat = new BingChat({
  cookie: process.env.BING_COOKIE
});

const anthropic = new Anthropic({
  apiKey: process.env.ANTHROPIC_API_KEY
});

// Conversation style mapping
const conversationStyles = {
  creative: 'Creative',
  balanced: 'Balanced',
  precise: 'Precise'
};

async function searchAndAnswer(question) {
  try {
    console.log('\n🔍 Searching the web for: "' + question + '"\n');
    
    // Perform web search using Bing Chat
    const searchResult = await bingChat.sendMessage(question, {
      variant: 'Balanced',
      conversationStyle: conversationStyles[options.conversationStyle] || 'Balanced'
    });
    
    console.log('✅ Search completed\n');
    console.log('🤖 Generating answer with Claude...\n');
    
    // Extract search results text
    const searchText = searchResult.text || '';
    const sourceAttributions = searchResult.sourceAttributions || [];
    
    // Create a comprehensive prompt for Claude
    const prompt = `You are a helpful AI assistant. Please answer the following question based on the web search results provided.

Question: ${question}

Web Search Results:
${searchText}

${sourceAttributions.length > 0 ? 'Sources:' : ''}
${sourceAttributions.map((source, index) => `${index + 1}. ${source.providerDisplayName || 'Source'}: ${source.seeMoreUrl || ''}`).join('\n')}

Please provide a comprehensive, accurate, and well-structured answer. Include relevant information from the search results and cite sources where appropriate. If the search results don't contain enough information to fully answer the question, please indicate that.`;

    // Generate answer using Claude
    const claudeResponse = await anthropic.messages.create({
      model: options.model,
      max_tokens: 4096,
      messages: [
        {
          role: 'user',
          content: prompt
        }
      ]
    });

    // Display the answer
    console.log('='.repeat(80));
    console.log('ANSWER');
    console.log('='.repeat(80));
    console.log('\n' + claudeResponse.content[0].text + '\n');
    
    // Display sources if available
    if (sourceAttributions.length > 0) {
      console.log('='.repeat(80));
      console.log('SOURCES');
      console.log('='.repeat(80));
      sourceAttributions.forEach((source, index) => {
        console.log(`${index + 1}. ${source.providerDisplayName || 'Unknown'}`);
        if (source.seeMoreUrl) {
          console.log(`   ${source.seeMoreUrl}`);
        }
      });
    }
    
    console.log('\n');
    
  } catch (error) {
    console.error('\n❌ Error:', error.message);
    
    if (error.message.includes('cookie')) {
      console.error('\nTip: Make sure your BING_COOKIE is valid and not expired.');
      console.error('You can get a new cookie by:');
      console.error('1. Going to bing.com and signing in');
      console.error('2. Opening DevTools (F12) → Application/Storage → Cookies');
      console.error('3. Copying the value of the "_U" cookie');
    }
    
    if (error.message.includes('401') || error.message.includes('Unauthorized')) {
      console.error('\nTip: Check that your ANTHROPIC_API_KEY is valid.');
    }
    
    process.exit(1);
  }
}

// Run the search
searchAndAnswer(question);
