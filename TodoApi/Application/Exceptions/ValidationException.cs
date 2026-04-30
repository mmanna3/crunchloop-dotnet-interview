namespace TodoApi.Application.Exceptions;

public class ValidationException(string message) : AppException(message, "validation_error") { }
