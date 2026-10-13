namespace Kaizen.Api.Common.Exceptions;

/// <summary>Base para exceções de domínio mapeadas para HTTP status pelo middleware.</summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public string ErrorTitle { get; }

    protected AppException(string title, string message) : base(message) => ErrorTitle = title;
}

/// <summary>404 - recurso não encontrado.</summary>
public class NotFoundException : AppException
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public NotFoundException(string message) : base("Recurso não encontrado", message) { }
}

/// <summary>409 - violação de unicidade / estado conflitante.</summary>
public class ConflictException : AppException
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public ConflictException(string message) : base("Conflito", message) { }
}

/// <summary>422 - regra de negócio violada.</summary>
public class BusinessRuleException : AppException
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
    public BusinessRuleException(string message) : base("Regra de negócio violada", message) { }
}

/// <summary>401 - falha de autenticação.</summary>
public class UnauthorizedAppException : AppException
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public UnauthorizedAppException(string message) : base("Não autorizado", message) { }
}
