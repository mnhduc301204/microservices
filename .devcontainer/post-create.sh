#!/bin/bash
set -e

echo "Restoring .NET packages..."
dotnet restore

echo "Building solution..."
dotnet build --no-restore

echo "Dev container setup complete!"
echo ""
echo "Quick start:"
echo "  dotnet run --project src/ECommerce.AppHost  # Start Aspire orchestration"
echo "  dotnet test ECommerce.slnx                  # Run all tests"
