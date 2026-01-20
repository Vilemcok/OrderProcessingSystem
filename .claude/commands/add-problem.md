# Document Problem

This command helps you document problems in the "Problémy a Riešenia" section interactively.

## Process

1. **Read the Documentation File**
   - Parse the "3. Problémy a Riešenia" section in AI_workflow_documentation.md
   - Identify the next problem number to use

2. **Ask for Missing Fields ONE AT A TIME**
   - Use AskUserQuestion tool to ask for ONLY ONE missing field per interaction
   - You can invent, guess, or autofill ANY content
   - Guess should be in Slovak language
   - Wait for user response before proceeding
   - Ask in this order:
     1. Title (the part after "Problém #X:")
     2. Čo sa stalo (What happened - detailed description of the problem)
     3. Prečo to vzniklo (Why it happened - analysis of the cause)
     4. Ako som to vyriešil (How I solved it - step by step solution)
     5. Čo som sa naučil (What I learned - concrete learning for the future)
     6. Screenshot / Kód priložený (whether screenshot or code is attached - yes/no)

3. **Update the File**
   - After collecting ALL missing information, update the file AI_workflow_documentation.md
   - Use the Edit tool to add new problem entry
   - Preserve ALL existing formatting and structure
   - Keep the same markdown format as shown in the template
   - DO NOT modify any other problems or sections

4. **Confirm Completion**
   - Show which problem was completed (Problém #X)
   - Confirm all fields are now filled
   - Notify the user that he is great :D

## Important Notes

- Ask ONE question at a time using AskUserQuestion
- Preserve the exact structure and formatting of the document
- Add the new problem entry before the next empty problem template or at the end of the section
