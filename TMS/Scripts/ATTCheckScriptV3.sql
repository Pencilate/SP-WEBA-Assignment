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
    (( CONVERT(DATE, @StartDateTime) >= CONVERT(DATE, EffectiveStartDateTime) AND CONVERT(DATE, @EndDateTime) <= CONVERT(DATE, EffectiveEndDateTime))
    OR ( CONVERT(DATE, @StartDateTime) >= CONVERT(DATE, EffectiveStartDateTime) AND  CONVERT(DATE, @StartDateTime) <= CONVERT(DATE, EffectiveEndDateTime))
    OR ( CONVERT(DATE, @StartDateTime) <= CONVERT(DATE, EffectiveStartDateTime) AND CONVERT(DATE, @EndDateTime) >= CONVERT(DATE, EffectiveEndDateTime))
    OR ( CONVERT(DATE, @StartDateTime) <= CONVERT(DATE, EffectiveStartDateTime) AND CONVERT(DATE, @EndDateTime) >= CONVERT(DATE, EffectiveStartDateTime)))
    AND
    (( CONVERT(TIME, @StartDateTime) >= CONVERT(TIME, EffectiveStartDateTime) AND CONVERT(TIME, @EndDateTime) <= CONVERT(TIME, EffectiveEndDateTime))
    OR ( CONVERT(TIME, @StartDateTime) >= CONVERT(TIME, EffectiveStartDateTime) AND  CONVERT(TIME, @StartDateTime) < CONVERT(TIME, EffectiveEndDateTime))
    OR ( CONVERT(TIME, @StartDateTime) <= CONVERT(TIME, EffectiveStartDateTime) AND CONVERT(TIME, @EndDateTime) >= CONVERT(TIME, EffectiveEndDateTime))
    OR ( CONVERT(TIME, @StartDateTime) <= CONVERT(TIME, EffectiveStartDateTime) AND CONVERT(TIME, @EndDateTime) > CONVERT(TIME, EffectiveStartDateTime)))
)

GO

CREATE PROCEDURE uspAccountTimeTableCheckOverlapBeforeUpdate  @AccountTimeTableId int, @AccountRateId int, @DayOfWeek int, @StartDateTime datetime2, @EndDateTime datetime2
AS(
  SELECT * FROM AccountTimeTable WHERE 
    AccountTimeTable.AccountRateId = @AccountRateId AND
    AccountTimeTable.DayOfWeekNumber = @DayOfWeek AND
    (( CONVERT(DATE, @StartDateTime) >= CONVERT(DATE, EffectiveStartDateTime) AND CONVERT(DATE, @EndDateTime) <= CONVERT(DATE, EffectiveEndDateTime))
    OR ( CONVERT(DATE, @StartDateTime) >= CONVERT(DATE, EffectiveStartDateTime) AND  CONVERT(DATE, @StartDateTime) <= CONVERT(DATE, EffectiveEndDateTime))
    OR ( CONVERT(DATE, @StartDateTime) <= CONVERT(DATE, EffectiveStartDateTime) AND CONVERT(DATE, @EndDateTime) >= CONVERT(DATE, EffectiveEndDateTime))
    OR ( CONVERT(DATE, @StartDateTime) <= CONVERT(DATE, EffectiveStartDateTime) AND CONVERT(DATE, @EndDateTime) >= CONVERT(DATE, EffectiveStartDateTime)))
    AND
    (( CONVERT(TIME, @StartDateTime) >= CONVERT(TIME, EffectiveStartDateTime) AND CONVERT(TIME, @EndDateTime) <= CONVERT(TIME, EffectiveEndDateTime))
    OR ( CONVERT(TIME, @StartDateTime) >= CONVERT(TIME, EffectiveStartDateTime) AND  CONVERT(TIME, @StartDateTime) < CONVERT(TIME, EffectiveEndDateTime))
    OR ( CONVERT(TIME, @StartDateTime) <= CONVERT(TIME, EffectiveStartDateTime) AND CONVERT(TIME, @EndDateTime) >= CONVERT(TIME, EffectiveEndDateTime))
    OR ( CONVERT(TIME, @StartDateTime) <= CONVERT(TIME, EffectiveStartDateTime) AND CONVERT(TIME, @EndDateTime) > CONVERT(TIME, EffectiveStartDateTime)))
    AND AccountTimeTable.AccountTimeTableId != @AccountTimeTableId
)

GO