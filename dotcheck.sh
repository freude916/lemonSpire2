#!/bin/bash
dotnet build -tl --nologo /clp:ErrorsOnly /p:IncludeDiagnosticRuleLink=false
