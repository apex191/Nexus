# Quick latency test script for Nexus
Write-Host "🚀 Running Nexus Ultra-Low Latency Benchmarks" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

Write-Host "`n📊 Running latency-focused benchmarks..." -ForegroundColor Yellow
cd Nexus.Benchmarks
dotnet run -c Release -- --filter "*Latency*"

Write-Host "`n📈 Performance optimization tips:" -ForegroundColor Cyan
Write-Host "- Review Mean/Median latency values" -ForegroundColor White  
Write-Host "- Check memory allocation patterns" -ForegroundColor White
Write-Host "- Look for GC pressure indicators" -ForegroundColor White
Write-Host "- Compare Min vs Max for consistency" -ForegroundColor White
