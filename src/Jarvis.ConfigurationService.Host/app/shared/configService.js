(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.shared')
            .factory('configService', configService);

    configService.$inject = ['$http','$q'];

    function configService($http,$q) {
        var service = {
            getStatus: getStatus
        };

        return service;

        var statusCache;

        /* */
        function getStatus() {
       
            var d = $q.defer();

            if (statusCache) {
                d.resolve(statusCache);
            }
            else {
                $http.get('/status').then(function (r) {
                    statusCache = r.data;
                    d.resolve(statusCache);
                }, function (err) {
                    d.reject(err);
                });
            }

            return d.promise;
        }
    }

})(window, window.angular);
