# Document Last Prompt

This command helps you document prompts in the "Zbierka Promptov" section interactively.

## Process

1. **Read the Documentation File**
   - Parse the "2. Zbierka Promptov" section in file C:\Workspace_Vipo\zadanie-dokument\priloha-c-sablona.md   
   - This file is only for formatting and structure reference;

2. **Ask for Missing Fields ONE AT A TIME**
   - Use AskUserQuestion tool to ask for ONLY ONE missing field per interaction
   - You can invent, guess, or autofill ANY content
   - Guess should be in Slovak language
   - Wait for user response before proceeding
   - Ask in this order:
     1. Title (the part after "Prompt #X:") (add behind title duration of the prompt and also how many % of Current session usage was spend by this prompt)
     2. Nástroj (Tool)
     3. Kontext (Context)
     4. Prompt (the actual prompt text)
     5. Výsledok (Result rating)
     6. Úpravy/Fixes (what needed to be fixed)
     7. Poznámky/Learnings (notes and learnings)

3. **Update the File**
   - After collecting ALL missing information, update the file AI_workflow_documentation.md
   - Use the Edit tool to add new prompt 
   - Preserve ALL existing formatting and structure
   - Keep the same markdown format as shown in the template
   - DO NOT modify any other prompts or sections

4. **Confirm Completion**
   - Show which prompt was completed (Prompt #X)
   - Confirm all fields are now filled
   - Notify the user that he is greate :D

## Important Notes

- Ask ONE question at a time using AskUserQuestion
- Preserve the exact structure and formatting of the document

