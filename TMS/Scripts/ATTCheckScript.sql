USE TMSDB_1828823

IF EXISTS  (SELECT * FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'uspAccountTimeTableCheckIdenticalBeforeAdd') )
  DROP PROCEDURE uspAccountTimeTableCheckIdenticalBeforeAdd;

GO

IF EXISTS (SELECT * FROM sys.objects 
     WHERE object_id = OBJECT_ID(N'uspAccountTimeTableCheckIdenticalBeforeUpdate') )
  DROP PROCEDURE uspAccountTimeTableCheckIdenticalBeforeUpdate;

GO 

IF EXISTS  (SELECT * FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'uspAccountTimeTableCheckOverlapBeforeAdd') )
  DROP PROCEDURE uspAccountTimeTableCheckOverlapBeforeAdd;

GO

IF EXISTS (SELECT * FROM sys.objects 
     WHERE object_id = OBJECT_ID(N'uspAccountTimeTableCheckOverlapBeforeUpdate') )
  DROP PROCEDURE uspAccountTimeTableCheckOverlapBeforeUpdate;

GO 

CREATE PROCEDURE uspAccountTimeTableCheckIdenticalBeforeAdd @AccountRateId int, @DayOfWeek int, @StartDateTime datetime2, @EndDateTime datetime2
AS(
  SELECT * FROM AccountTimeTable WHERE 
    AccountTimeTable.AccountRateId = @AccountRateId AND
    AccountTimeTable.DayOfWeekNumber = @DayOfWeek AND
    AccountTimeTable.EffectiveStartDateTime = @StartDateTime AND
    AccountTimeTable.EffectiveEndDateTime = @EndDateTime
)

GO

CREATE PROCEDURE uspAccountTimeTableCheckIdenticalBeforeUpdate @AccountTimeTableId int, @AccountRateId int, @DayOfWeek int, @StartDateTime datetime2, @EndDateTime datetime2
AS(
  SELECT * FROM AccountTimeTable WHERE 
    AccountTimeTable.AccountRateId = @AccountRateId AND
    AccountTimeTable.DayOfWeekNumber = @DayOfWeek AND
    AccountTimeTable.EffectiveStartDateTime = @StartDateTime AND
    AccountTimeTable.EffectiveEndDateTime = @EndDateTime AND
    AccountTimeTable.AccountTimeTableId != @AccountTimeTableId
)

GO

CREATE PROCEDURE uspAccountTimeTableCheckOverlapBeforeAdd @AccountRateId int, @DayOfWeek int, @StartDateTime datetime2, @EndDateTime datetime2
AS(
  SELECT * FROM AccountTimeTable WHERE 
    AccountTimeTable.AccountRateId = @AccountRateId AND
    AccountTimeTable.DayOfWeekNumber = @DayOfWeek AND
    ((@StartDateTime >= EffectiveStartDateTime AND @EndDateTime <= EffectiveEndDateTime)
    OR (@StartDateTime >= EffectiveStartDateTime AND @StartDateTime <= EffectiveEndDateTime)
    OR (@StartDateTime <= EffectiveStartDateTime AND @EndDateTime >= EffectiveEndDateTime)
    OR (@StartDateTime <= EffectiveStartDateTime AND @EndDateTime >= EffectiveStartDateTime))
)

GO

CREATE PROCEDURE uspAccountTimeTableCheckOverlapBeforeUpdate  @AccountTimeTableId int, @AccountRateId int, @DayOfWeek int, @StartDateTime datetime2, @EndDateTime datetime2
AS(
  SELECT * FROM AccountTimeTable WHERE 
    AccountTimeTable.AccountRateId = @AccountRateId AND
    AccountTimeTable.DayOfWeekNumber = @DayOfWeek AND
    ((@StartDateTime >= EffectiveStartDateTime AND @EndDateTime <= EffectiveEndDateTime)
    OR (@StartDateTime >= EffectiveStartDateTime AND @StartDateTime <= EffectiveEndDateTime)
    OR (@StartDateTime <= EffectiveStartDateTime AND @EndDateTime >= EffectiveEndDateTime)
    OR (@StartDateTime <= EffectiveStartDateTime AND @EndDateTime >= EffectiveStartDateTime)) AND
    AccountTimeTable.AccountTimeTableId != @AccountTimeTableId
)

GO