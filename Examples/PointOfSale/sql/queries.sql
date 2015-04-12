/*

select * from dbo.TransportAudit
select * from dbo.TransportAudit where success=1
select * from dbo.TransportAudit where channel = 'get-invoice-summary'
select max(attempt) from dbo.TransportAudit where success=0
select distinct failedaction from dbo.TransportAudit 
select * from dbo.transportaudit where failedaction = 'finish'
select * from dbo.transportaudit where exception is not null and exception not like '%nemesis%'
select * from dbo.transportaudit order by handlerstarted desc

select distinct failedaction from dbo.transportaudit
select *, getutcdate() from dbo.TransportAudit where handlerfinished is null
delete from dbo.TransportAudit

declare @start datetime
declare @end datetime
select @start = dateadd(minute, -5, getutcdate()) -- look at past 5 minutes
select @start = min(publishtimestamp) from dbo.TransportAudit with(nolock) where handlerstarted >= @start
select @end = max(handlerfinished) from dbo.TransportAudit with(nolock) where handlerstarted >= @start
declare @duration time(3) = cast((@end - @start) as time)
declare @total_seconds int = datepart(second, @duration) + 60 * datepart(minute, @duration) + 3600 * datepart(hour, @duration)
declare @count int
select @count = count(*) from dbo.TransportAudit with(nolock) where handlerstarted >= @start
select @count as Count, @duration as Duration, @count/convert(real, @total_seconds) as [Processed Per Second]

select * from dbo.TransportAudit order by messageid
select * from dbo.TransportAudit order by publishtimestamp desc
select * from dbo.TransportAudit order by dequeuetime desc
select * from dbo.TransportAudit order by processtime desc
select * from dbo.TransportAudit where attempt > 1

select * from dbo.TransportAudit 
where messageid in
(select messageid from dbo.TransportAudit 
where Success = 0)
--and channel = 'get-invoice-details'
--and success = 0
order by handlerstarted desc

*/

/*

drop procedure dbo.spTransportAudit_InsertUpdate
drop table dbo.TransportAudit

*/
