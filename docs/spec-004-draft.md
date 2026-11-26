# Specification 004: Draft

## Overview

This spec will add four features to the application:
- An "About" page
- Home Page Enhancements
- Application State 
- Convert Page Enhancement

---

## 1. About Page

Add an "About" page to the application.
It should appear on the Top-Nav bar as the last item.

The initial content is shown below:

```
# About

The **TcxTool** application was developed by **Chris Joakim** as an
experiment of using the **Software Factory** approach to develop software
in an entirely hands-off manner using **Claude Code** and **Spec-Kit**.

## Spec-Kit Process 

## Spec-Kit Project Startup

- /speckit-constitution - Establish project principles  

## Spec-Kit Feature/Spec Development Loop
                                                       │
- /speckit-specify - Create baseline specification                                                             │
- /speckit-plan - Create implementation plan                                                                   │
- /speckit-tasks - Generate actionable tasks                                                                   │
- /speckit-implement - Execute implementation                                                                  │
- /speckit-converge - Assess the codebase and append remaining work as tasks  

## Links 

- [speckit.org](https://speckit.org)
- [spec-kit @ GitHub](https://github.com/github/spec-kit)
- [Spec-Kit Quickstart](https://github.github.io/spec-kit/quickstart.html)
- [specify-cli @ PyPI](https://pypi.org/project/specify-cli/)
- [GitHub repo for this project](https://github.com/cjoakim/cj-speckit-explore)
```

---

## 2. Home Page Enhancements

The page currently has a box named "Convert TCX to CSV".

Add a similarly-sized box, to the right, named "Select CSV File".
This will allow the user to use a file-selector to select a CSV file
previously created by this application.

---

## 3. Application State 

The CSV filename converted or selected on the Home page should be 
stored in the application state JSON file, named `tcxtool-state.json`
in the tcxtool/ directory.

The file should have just one key, `csvFilename`, with the value being
the filename of the CSV file either converted or selected.

The other pages in application will use this application state 
file for their processing.

---

## 4. Convert Page Enhancement

After the TCX file is converted to CSV, the CSV filename should be stored
in the application state JSON file at the key `csvFilename`.
