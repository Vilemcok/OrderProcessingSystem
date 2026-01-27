# AI Workflow Dokumentácia

**Meno:** 
Viliam Póčik

**Dátum začiatku:** 
19.1.2026

**Dátum dokončenia:** 
23.1.2026

**Zadanie:** / Backend

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Cursor IDE:** _____ hodín
- [ ] **Claude Code:** _8___ hodín  
- [ ] **GitHub Copilot:** _____ hodín
- [ ] **ChatGPT:** _2___ hodín
- [ ] **Claude.ai:** _____ hodín
- [ ] **Iné:** 

**Celkový čas vývoja (približne):** __12__ hodín

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
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[x] ❌ Nefungoval, musel som celé prepísať

**Čo som musel upraviť / opraviť:**
```
Neprepisoval som nič, ale musel som odklikávať, čo som dúfal, že už nebudem musieť.
```

**Poznámky / Learnings:**
```
Neviem, či je to vôbec možné, no už som nehľadal inú cestu, ako to dosiahnuť. Teraz mi to už príde aj ako zlý nápad.
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
Nič
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
add projet to remote https://github.com/Vilemcok/OrderProcessingSystem.git. Do first commit. Think about gitignore.
```

**Výsledok:**
[x] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Nič
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
Nič
```

**Poznámky:**
```
Tu som pri mojom commande add-prompt vybral možnosť, kde som mu mohol napísať, že chcem niečo inak. Napísal som mu, nech pridá viac detailov. Výsledok je obšírny až prekvapujúci. To, že dal hodnotu 9/10, opísal ako „one-pass implementation success“, čo znamená, že iný AI agent by mal byť schopný implementovať celý systém bez ďalších otázok. Toto je presne to, čo context engineering sľubuje – samovalidujúce sa PRP s dostatočným kontextom pre autonómnu implementáciu. (jeho slová)
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
Podrobnejší opis je v problem 1. Ponúkol mi že môžem niečo napísať, tak som mu zadal prompt aby nepoužíval using Microsoft.AspNetCore.OpenApi.
Login mi nefungoval lebo pravdepodobne naseedoval sql tabulku s nie realnym hashom. O taký stĺpec som ho ani nežiadal a keďže v zadaní soľ a hash nebola spomínaná zadal som nasledujúci prompt.
```

**Poznámky:**
```
Executívny agent dobre zvládol 40+ taskov, ale .NET 10 kompatibilita vyžadovala zásah.
```



### Prompt #7: Migrácia na plain text password authentication (~15 min, 19% usage)

**Nástroj:** Claude Code
**Kontext:** Database migration a autentifikácia

**Prompt:**
```
Add migration to remove PasswordHash column in User table and add new column Password. Seed it with real passwords of our two users. Than change logic to make authorization work. Mainly LoginAsync method in AuthService service.
```

**Výsledok:**
[x] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Úpravy:**
```
Nič.
```

**Poznámky:**
```
AI dobre zvládol kompletný workflow: zmena entity, migrácia DB, update služieb, testovanie. Všetko autonómne.
```



### Prompt #8: Generovanie PRP pre Part 2 (Event-Driven Architecture) (~25-30 min, 33% usage)

**Nástroj:** Claude Code
**Kontext:** PRP generovanie a research (Event-Driven Architecture)

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
Nič.
```

**Poznámky:**
```
Context engineering metóda vynikajúco funguje - AI autonómne vykonala research, analyzú codebase a vytvorila PRP s confidence 9/10
```

---

## 3. Problémy a Riešenia 

> 💡 **Tip:** Problémy sú cenné! Ukazujú ako riešiš problémy s AI.

### Problém #1: Pokus s Promptom #1 aby boli pre-approved

**Čo sa stalo:**
```
Napriek rady od Chat-GPT tento pokus nefungoval.
```

**Prečo to vzniklo:**
```
Už som to neriešil. Ale funguje moj command "add-problem", ktorý práve používam :)
```

**Ako som to vyriešil:**
```
Nevyriešil a ani to v budúcnu neplánujem poúžívať, lebo by to mohlo byť nebezpečné.
```

**Čo som sa naučil:**
```
Nie všetko funguje ako session rule. Keby som mal.
```

**Screenshot / Kód:** [ ] Priložený

---

### Problém #2: Swashbuckle vs Microsoft.AspNetCore.OpenApi

**Čo sa stalo:**
```
Cloud sa dostal do cyklu s pridávaním a odstraňovaním referencie a nefungoval mu build.
```

**Prečo:**
```
 Claude pridal Microsoft.AspNetCore.OpenApi, ale to malo byt pridané cez Swashbuckle.AspNetCore.
```

**Riešenie:**
```
Ponukol mi že mozem nieco napisat tak som napisal nech nepouziva Microsoft.AspNetCore.OpenApi
```

**Learning:**
```
Neviem, či by som sa vedel vyhnúť takémuto prípadom počas tvorenia INITIAL.md. Možno áno ale chcelo by to pár testovaní za účelom nájdenia správneho výrazu a odsledovania, či to pomohlo.
```

---

### Problém #3: Testy nie vo vlastnom projekte

**Čo sa stalo:**
```
Claude vytvoril testy, ale umiestnil ich do hlavného priečinku projektu (OrderProcessingSystem/) namiesto samostatného test projektu. Štruktúra bola nesprávna, pretože OrderProcessingSystem.Tests.csproj bol v OrderProcessingSystem/OrderProcessingSystem.Tests/, čo kolidovalo s hlavným OrderProcessingSystem.csproj v OrderProcessingSystem/.
```

**Prečo:**
```
AI nesprávne interpretovalo štruktúru solution a umiestnilo test projekt do nesprávneho priečinka, čo spôsobilo konflikt názvov a projektových súborov.
```

**Riešenie:**
```
Išiel som cez takyto obšírny popis do ChatGPT:

Poprosil som ChatGPT aby mi pomohol sformulovať správny prompt pre Claude Code:

"potrebujem zadať pre claude code inštrukcie aby poprehadzoval solution lebo nemôže byť OrderProcessingSystem.Tests.csproj ktorý je v OrderProcessingSystem/OrderProcessingSystem.Tests priečinku pretože vo OrderProcessingSystem priečinku je OrderProcessingSystem.csproj. Nech mi spraví OrderProcessingSystem/OrderProcessingSystem.WebApi kde bude hlavný projekt a OrderProcessingSystem/OrderProcessingSystem.Tests kde bude test projekt"

ChatGPT mi vygeneroval štruktúrovaný prompt (ako mini PRP), ktorý som poslal Claude Code a všetko sa reorganizovalo správne.
```

**Learning:**
```
Použitie ChatGPT na generovanie jasných, štruktúrovaných promptov pre Claude Code je efektívna stratégia. Keď má Claude problém s pochopením komplexnej reorganizácie, ChatGPT môže pomôcť sformulovať lepší prompt.
```

## 4. Kľúčové Poznatky

### 4.1 Čo fungovalo výborne

**1.** 
```
Fantasticky fungovala druhá časť. Tú zbehol bez zásahu a keď som si to otestoval tak všetko išlo. Najskôr som sa divil prečo tam zas nedal testy, ale uvedomil som si že som mu to nezadal lebo to nebolo v zadaní. 
```

**2.**
```
Páči sa mi, že keď má s niečím problém tak nemíňa % a neexperimentuje donekonečna. Opýta sa na ďalší krok s vysvetlením situácie a potom pokračuje v plnení.
```

### 4.2 Čo bolo náročné

**1.**
```
Náročné bolo vidieť ako rýchlo sa míňajú %. Jednoduchá otázka 2%. Zložitejšia 4%. Volanie mojich commandov na pridávanie promptov alebo problémov 6-11%. Ešte stresujúcejšie bolo, že oproti videám pribudol aj týždenný limit.
```

---

### 4.3 Best Practices ktoré som objavil

**1.** 
```
Čo najviac si premyslieť prvý prompt. Mne nezapracoval testy do osobitného projektu takže som to musel riešiť ďaľším promptom. Ale celkovo stále o nič nešlo. 
```

**2.** 
```
Odkladanie si promptom je myslím užitočné aj keby nešlo o vypracovávanie dokumentácie kvôli certifikácii.
```

**3.** 
```
Radšej o bežnejších otázkach a riešeniach používať zadarmo ChatGPT. Claude v terminaly už iba na prácu s hotovými pripravenými premyslenými promptami.
```


---

### 4.4 Moje Top 3 Tipy Pre Ostatných

**Tip #1:**
```
Ku koncu som sa začal hrať s integrovanim ChatGPT priamo pre neho ale nebol čas to dotiahnuť. Ide o to, že niektoré zaseknutia alebo zaciklenia by po vysvetlením mohol najskôr smerovať na ChatGPT a mal by zvážiť radu. Až potom smerovať otázky na nás programátorov. Jeho zhrnutie do CLAUDE.md:

Automatically consult ChatGPT in these scenarios:
1. **Build errors** - Compilation errors, missing dependencies, type errors
2. **Runtime errors** - Exceptions, crashes, unexpected behavior
3. **Configuration issues** - Problems with appsettings, environment variables, or service registration
4. **Test failures** - When tests fail and the cause is not immediately clear
5. **Integration problems** - Issues with databases, message queues, external APIs
6. **Unknown errors** - Any error message or problem you're uncertain how to solve

```

**Tip #2:**
```
Mám subjektívny dojem, že opísať viac slovami obšírnejšie, čo chceš dosiahnuť je lepšie ako snažiť sa byť struční a výstižný. Takže omáčky nevadia. AI má väčší zmysel pre pochopenie, keď má dobre vysvetľujúci vstup. Asi je to chlap a treba mu opísať problém z viacerých strán namiesto očakávania, že niečo pochopí medzi riadkami.
```

**Tip #3:**
```
Investuj čas do prípravy kvalitného INITIAL.md a PRP dokumentu. Context engineering metóda ukázala svoju silu - dobre pripravený PRP dokument umožnil Claude Code autonómne implementovať celý systém. Čím lepšie pripravíš kontext a štruktúru, tým menej musíš zasahovať počas implementácie.
```

---

## 6. Reflexia a Závery

### 6.1 Efektivita AI nástrojov

**Ktorý nástroj bol najužitočnejší?** ____CLAUDE CODE_____________________________

**Prečo?**
```
Pretože to celé spravil.
```

**Ktorý nástroj bol najmenej užitočný?** ___ChatGPT______________________________

**Prečo?**
```
ChatGPT som využíval primárne ako konzultačný nástroj na formulovanie promptov a analýzu problémov, zatiaľ čo samotnú implementáciu vykonával Claude Code. Takže v rámci dvoch bol ten menej užitočný.
```

---

### 6.2 Najväčšie prekvapenie
```
Pri druhej časti ma context engineering metóda prekvapila. Ukázala svoju skutočnú silu pri generovaní komplexného PRP dokumentu. AI automaticky vykonala 5 web searchov pre aktuálne best practices (.NET 10, JWT auth, Testcontainers, EF Core s PostgreSQL) a integrovala výsledky priamo do PRP. Vygenerovaný dokument obsahoval nielen implementačné kroky, ale aj konkrétne príklady kódu, linky na oficiálnu dokumentáciu a kopec ďaľšieho. Najdôležitejšie je, že PRP dostal skóre 9/10
```

---

### 6.3 Najväčšia frustrácia
```
Z míňania %. Na serióznu robotu by bolo pravdepodobne treba zakúpiť Max za $100. Ďaľšia frustrácia je, že claude code nemôžem používať na projekte, na ktorom práve pracujem.
```

---

### 6.4 Najväčší "AHA!" moment
```
Môj AHA moment bol, keď som prvý krát videl ako claude na pár príkazov pracuje už na videách kurzu a praktickej časti. Teraz pri vypracovávaní zadania som si to len užíval. 
```

---

### 6.5 Čo by som urobil inak
```
Robil by som si z kurzových videí priebežne poznámky. Chvalabohu za zrýchlený režim ktorý je užitočný, keď som potreboval niečo nájsť, čo som si nepamätal úplne.
```

### 6.6 Hlavný odkaz pre ostatných
```
Treba sa naším zamestnávateľom čím skôr snažiť vysvetliť aký by bol prínos keby dovolili používať claude code.
```
