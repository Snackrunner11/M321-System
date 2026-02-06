import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((err) => {
      // Das hier erzeugt das sichtbare Fenster fuer den Screenshot
      alert('ALARM: Der Server ist nicht erreichbar! Fehler Code: ' + err.status);
      return throwError(() => err);
    })
  );
};