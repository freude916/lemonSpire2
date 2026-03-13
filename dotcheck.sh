#!/bin/bash
dotnet build \
  --no-restore \
  -p:ProduceOnlyReferenceAssembly=true \
  -p:GenerateDocumentationFile=false \
  -tl
