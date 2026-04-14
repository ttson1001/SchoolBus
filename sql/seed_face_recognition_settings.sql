IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[SystemSettings]')
      AND type = N'U'
)
BEGIN
    PRINT N'Bang [dbo].[SystemSettings] chua ton tai. Hay tao migration va update database truoc.';
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM [dbo].[SystemSettings]
    WHERE [Key] = N'FaceRecognition.SimilarityThreshold'
)
BEGIN
    UPDATE [dbo].[SystemSettings]
    SET
        [Value] = N'0.8',
        [Description] = N'Nguong do giong cho face recognition'
    WHERE [Key] = N'FaceRecognition.SimilarityThreshold';
END
ELSE
BEGIN
    INSERT INTO [dbo].[SystemSettings] ([Key], [Value], [Description])
    VALUES (
        N'FaceRecognition.SimilarityThreshold',
        N'0.8',
        N'Nguong do giong cho face recognition'
    );
END;

SELECT TOP 1 *
FROM [dbo].[SystemSettings]
WHERE [Key] = N'FaceRecognition.SimilarityThreshold';
