namespace TodoApi.Application.Exceptions;

public class NotFoundException(string message) : AppException(message, "not_found") { }
