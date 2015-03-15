for ($i=1; $i -le 50; $i++)
{
    Invoke-RestMethod -Uri http://localhost:4151/pub?topic=pos.customer.cmd.get-all -Method Post -Body "{}"
    Invoke-RestMethod -Uri http://localhost:4151/pub?topic=pos.invoice.cmd.get-all -Method Post -Body "{}"
    Invoke-RestMethod -Uri http://localhost:4151/pub?topic=pos.product.cmd.get-all -Method Post -Body "{}"
}
