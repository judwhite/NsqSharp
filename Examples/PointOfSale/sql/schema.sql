create table dbo.TransportAudit
(
  TransportAuditId uniqueidentifier not null,
  Topic varchar(64) not null,
  Channel varchar(64) not null,
  HandlerType nvarchar(256) not null,
  MessageType nvarchar(256) not null,
  MessageId varchar(16) not null,
  Attempt int not null,
  NsqdAddress nvarchar(256) not null,
  Body nvarchar(max),
  PublishTimestamp datetime not null,
  HandlerStarted datetime not null,
  HandlerFinished datetime,
  DequeueTime time(3),
  ProcessTime time(3),
  Success bit,
  FailedAction nvarchar(20),
  FailedReason nvarchar(20),
  Exception nvarchar(max)
)
go

alter table dbo.TransportAudit
add constraint PK_TransportAudit_TransportAuditId primary key (TransportAuditId)
go

create index IX_TransportAudit_Topic on dbo.TransportAudit (Topic)
create index IX_TransportAudit_Channel on dbo.TransportAudit (Channel)
create index IX_TransportAudit_MessageId on dbo.TransportAudit (MessageId)
create index IX_TransportAudit_NsqdAddress on dbo.TransportAudit (NsqdAddress)
create index IX_TransportAudit_PublishTimestamp on dbo.TransportAudit (PublishTimestamp)
create index IX_TransportAudit_HandlerStarted on dbo.TransportAudit (HandlerStarted)
create index IX_TransportAudit_HandlerFinished on dbo.TransportAudit (HandlerFinished)
create index IX_TransportAudit_DequeueTime on dbo.TransportAudit (DequeueTime)
create index IX_TransportAudit_ProcessTime on dbo.TransportAudit (ProcessTime)
create index IX_TransportAudit_Success on dbo.TransportAudit (Success)
create index IX_TransportAudit_FailedAction on dbo.TransportAudit (FailedAction)
go

create procedure dbo.spTransportAudit_InsertUpdate
  @TransportAuditId uniqueidentifier,
  @Topic varchar(64),
  @Channel varchar(64),
  @HandlerType nvarchar(256),
  @MessageType nvarchar(256),
  @MessageId varchar(16),
  @Attempt int,
  @NsqdAddress nvarchar(256),
  @Body nvarchar(max),
  @PublishTimestamp datetime,
  @HandlerStarted datetime,
  @HandlerFinished datetime,
  @Success bit,
  @FailedAction nvarchar(20),
  @FailedReason nvarchar(20),
  @Exception nvarchar(max)
as

  declare @TimeInQueueMilliseconds int
  declare @ProcessingMilliseconds int
  declare @DequeueTime time(3)
  declare @ProcessTime time(3)

  set @TimeInQueueMilliseconds = datediff(millisecond, @PublishTimestamp, @HandlerStarted)
  if @TimeInQueueMilliseconds > 86400000
  begin
    set @DequeueTime = cast('24:00:00' as time(3))
  end
  else
  begin
    set @DequeueTime = dateadd(millisecond, @TimeInQueueMilliseconds, cast('00:00:00' as time(3)))
  end

  if @HandlerFinished is not null
  begin
    set @ProcessingMilliseconds = datediff(millisecond, @HandlerStarted, @HandlerFinished)
    if @ProcessingMilliseconds > 86400000
    begin
      set @ProcessTime = cast('24:00:00' as time(3))
    end
    else
    begin
      set @ProcessTime = dateadd(millisecond, @ProcessingMilliseconds, cast('00:00:00' as time(3)))
    end
  end

  merge dbo.TransportAudit with(holdlock) t
  using (select @TransportAuditId as TransportAuditId) s
  on t.TransportAuditId = s.TransportAuditId
  when not matched by target then
    insert
	(
      TransportAuditId,
      Topic,
      Channel,
      HandlerType,
      MessageType,
      MessageId,
      Attempt,
      NsqdAddress,
      Body,
      PublishTimestamp,
      HandlerStarted,
      HandlerFinished,
      DequeueTime,
      ProcessTime,
	  Success,
	  FailedAction,
	  FailedReason,
	  Exception
    )
	values
	(
      @TransportAuditId,
      @Topic,
      @Channel,
      @HandlerType,
      @MessageType,
      @MessageId,
      @Attempt,
      @NsqdAddress,
      @Body,
      @PublishTimestamp,
      @HandlerStarted,
      @HandlerFinished,
      @DequeueTime,
      @ProcessTime,
	  @Success,
	  @FailedAction,
	  @FailedReason,
	  @Exception
	)
  when matched then
    update set
	  t.HandlerFinished = isnull(@HandlerFinished, t.HandlerFinished),
	  t.DequeueTime = isnull(@DequeueTime, t.DequeueTime),
	  t.ProcessTime = isnull(@ProcessTime, t.ProcessTime),
	  t.Success = isnull(@Success, t.Success),
	  t.FailedAction = isnull(@FailedAction, t.FailedAction),
	  t.FailedReason = isnull(@FailedReason, t.FailedReason),
	  t.Exception = isnull(@Exception, t.Exception);

go
