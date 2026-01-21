# AI Workflow Dokumentácia

**Meno:** 

**Dátum začiatku:** 

**Dátum dokončenia:** 

**Zadanie:** Frontend / Backend

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Cursor IDE:** _____ hodín
- [ ] **Claude Code:** _____ hodín  
- [ ] **GitHub Copilot:** _____ hodín
- [ ] **ChatGPT:** _____ hodín
- [ ] **Claude.ai:** _____ hodín
- [ ] **Iné:** 

**Celkový čas vývoja (priližne):** _____ hodín

---

## 2. Zbierka Promptov

> 💡 **Tip:** Kopíruj presný text promptu! Priebežne dopĺňaj po každej feature.

### Prompt #1: instructions are pre-approved (~5-7 min)

**Nástroj:** Claude Code
**Kontext:** Dokumentácia a workflow

**Prompt:**
```
Session rule: All changes aligned with my instructions are pre-approved. Do not ask for write confirmations in this session.
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[x] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Čo som musel upraviť / opraviť:**
```
Neprepisoval som nič ale musel som odklikávať čo som dúfal že už nebudem musieť
```

**Poznámky / Learnings:**
```
Asi som sa opýtal zle, no možno v ďalších commandoch toho budem menej na odklikávanie
```



### Prompt #2: Setup context engineering systému (~3-4 min)

**Nástroj:** Claude Code
**Kontext:** Dokumentácia a workflow

**Prompt:**
```
as is on page https://github.com/coleam00/context-engineering-intro create Execute.prp.md Generate-prp.md PRPs Directory and INITIAL.md and commands directory. Nothing more
```

**Výsledok:**
[x] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Nič, fungoval perfektne
```

**Poznámky:**
```
Odkaz na GitHub ako reference je efektívne - AI presne vytvorilo požadovanú štruktúru bez zbytočných doplnkov
```



### Prompt #3: Vytvorenie add-problem commandu (~5-10 min usage 46%!)

**Nástroj:** Claude Code
**Kontext:** Dokumentácia a workflow

**Prompt:**
```
Generate for me new command with name "add-problem" which will allow me to add problem. It will work the simular way as existing command "add-prompt" but it will add new "problem" to "3. Problémy a Riešenia" part of AI_workflow_documentation.md
```

**Výsledok:**
[x] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Nič, fungoval perfektne
```

**Poznámky:**
```
AI dobre rozumie štruktúre existujúcich commandov
```



### Prompt #4: Git setup a prvý commit (~8-12 min 55%)

**Nástroj:** Claude Code
**Kontext:** Git a verziovanie

**Prompt:**
```
add projet to remote https://github.com/Vilemcok/OrderProcessingSystem.git. Do first commint. Think about gitignore.
```

**Výsledok:**
[x] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Nič, fungoval perfektne
```

**Poznámky:**
```
AI automaticky vytvorila kompletný .gitignore pre .NET
```



### Prompt #5: Generovanie kompletného PRP pre OrderProcessingSystem (~25-30 min, 23% usage)

**Nástroj:** Claude Code
**Kontext:** PRP generovanie a research

**Prompt:**
```
/generate-prp INITIAL.md
```

**Výsledok:**
[x] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Nič, fungoval perfektne
```

**Poznámky:**
```
Context engineering metóda ukázala svoju skutočnú silu pri generovaní komplexného PRP dokumentu. AI automaticky vykonala 5 web searchov pre aktuálne best practices (.NET 10, JWT auth, Testcontainers, EF Core s PostgreSQL) a integrovala výsledky priamo do PRP. Vygenerovaný dokument obsahuje nie len implementačné kroky, ale aj konkrétne príklady kódu, linky na oficiálnu dokumentáciu, validation gates a 40+ checkpoint taskov. Najdôležitejšie je, že PRP dostal skóre 9/10 na "one-pass implementation success", čo znamená, že iný AI agent by mal byť schopný implementovať celý systém bez ďalších otázok. Toto je presne to, čo context engineering sľubuje - samovalidujúce sa PRP s dostatočným kontextom pre autonómnu implementáciu.
```



### Prompt #6: Implementácia kompletného Order Processing System cez /execute-prp (~95 min, 56% usage)

**Nástroj:** Claude Code
**Kontext:** Autonómna implementácia z PRP dokumentu

**Prompt:**
```
/execute-prp order-processing-system-implementation.md
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)
[x] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Swagger + statické seed data + package versions
```

**Poznámky:**
```
Executívny agent dobre zvládol 40+ taskov, ale .NET 10 kompatibilita vyžadovala zásahy
```

---

## 3. Problémy a Riešenia 

> 💡 **Tip:** Problémy sú cenné! Ukazujú ako riešiš problémy s AI.

### Problém #1: Pokus s Promptom #1 aby boli pre-approved

**Čo sa stalo:**
```
Napriek rady od Chat-GPT tento pokus nefungoval
```

**Prečo to vzniklo:**
```
Doteraz som to nevyriesil. Len prvy krat skusam prompt "add-problem"
```

**Ako som to vyriešil:**
```
Dopisem neskor. Zatial neviem.
```

**Čo som sa naučil:**
```
Nie všetko funguje ako session rule
```

**Screenshot / Kód:** [ ] Priložený

---

### Problém #2: _________________________________

**Čo sa stalo:**
```
```

**Prečo:**
```
```

**Riešenie:**
```
```

**Learning:**
```
```

## 4. Kľúčové Poznatky

### 4.1 Čo fungovalo výborne

**1.** 
```
[Príklad: Claude Code pre OAuth - fungoval first try, zero problémov]
```

**2.** 
```
```

**3.** 
```
```

**[ Pridaj viac ak chceš ]**

---

### 4.2 Čo bolo náročné

**1.** 
```
[Príklad: Figma MCP spacing - často o 4-8px vedľa, musel som manuálne opravovať]
```

**2.** 
```
```

**3.** 
```
```

---

### 4.3 Best Practices ktoré som objavil

**1.** 
```
[Príklad: Vždy špecifikuj verziu knižnice v prompte - "NextAuth.js v5"]
```

**2.** 
```
```

**3.** 
```
```

**4.** 
```
```

**5.** 
```
```

---

### 4.4 Moje Top 3 Tipy Pre Ostatných

**Tip #1:**
```
[Konkrétny, actionable tip]
```

**Tip #2:**
```
```

**Tip #3:**
```
```

---

## 6. Reflexia a Závery

### 6.1 Efektivita AI nástrojov

**Ktorý nástroj bol najužitočnejší?** _________________________________

**Prečo?**
```
```

**Ktorý nástroj bol najmenej užitočný?** _________________________________

**Prečo?**
```
```

---

### 6.2 Najväčšie prekvapenie
```
[Čo ťa najviac prekvapilo pri práci s AI?]
```

---

### 6.3 Najväčšia frustrácia
```
[Čo bolo najfrustrujúcejšie?]
```

---

### 6.4 Najväčší "AHA!" moment
```
[Kedy ti došlo niečo dôležité o AI alebo o developmente?]
```

---

### 6.5 Čo by som urobil inak
```
[Keby si začínal znova, čo by si zmenil?]
```

### 6.6 Hlavný odkaz pre ostatných
```
[Keby si mal povedať jednu vec kolegom o AI development, čo by to bylo?]
```
